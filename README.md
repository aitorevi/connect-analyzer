# SAP Analyzer

[![CI](https://github.com/aitorevi/sap-analyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/aitorevi/sap-analyzer/actions/workflows/ci.yml)

Herramienta interna para **analizar grandes cantidades de datos provenientes de SAP** y visualizarlos
en un frontal con gráficos. Empieza a pequeña escala con datos simulados y se itera poco a poco: el
objetivo es tener pronto el flujo más fino de punta a punta (**un fichero `.txt` → un endpoint → un
gráfico**) y a partir de ahí engordar con más análisis, filtros y, solo después, persistencia y origen
real de SAP.

## Arquitectura

Tres piezas, cada una en su carpeta, orquestadas con **Docker Compose**:

```
┌────────────┐      HTTP       ┌────────────┐      HTTP/JSON   ┌────────────┐
│  frontend  │ ───────────────▶│  backend   │ ───────────────▶│  sap-mock  │
│  Next.js   │  /api/sales/... │  .NET 10   │   /sales.txt    │   nginx    │
│  :3000     │◀─────────────── │  :5080→8080│◀─────────────── │  :8000→8080│
└────────────┘                 └────────────┘                 └────────────┘
   gráficos                  API REST + análisis            export simulado de SAP
```

| Pieza        | Tecnología                         | Puerto (host→interno) | Rol                                                            |
|--------------|------------------------------------|-----------------------|----------------------------------------------------------------|
| `sap-mock/`  | nginx (unprivileged)               | `8000 → 8080`         | Origen de datos simulado. Sirve un `.txt` que imita un export de SAP. |
| `backend/`   | C# / **.NET 10** (Web API)         | `5080 → 8080`         | Lee del mock, procesa y sirve REST al front. Arquitectura hexagonal.  |
| `frontend/`  | **Next.js** (App Router) + Recharts| `3000`                | Consume la API y pinta gráficos.                               |

> El origen de datos está aislado tras el **puerto `ISalesRepository`** del backend. Hoy lo implementa un
> adaptador que lee del mock (`MockTxtSalesRepository`); mañana puede ser SAP real (OData, ficheros, RFC/BAPI)
> escribiendo un adaptador nuevo, **sin tocar el resto del sistema**.

## Stack

- **Backend**: C# con **.NET 10** (Web API), arquitectura hexagonal (Ports & Adapters), manejo de errores
  esperables con un tipo `Result<T>`/`Error` propio. Tests con **xUnit**.
- **Frontend**: **Next.js 16** + **TypeScript** (App Router) + **Recharts**. Tests con **Vitest** + React Testing Library.
- **Mock**: **nginx** sirviendo ficheros estáticos.
- **Orquestación**: **Docker** + **Docker Compose**.

## Requisitos previos

- **Docker** y **Docker Compose** (única dependencia obligatoria para levantar todo).
- *Opcional, solo para desarrollo nativo*: **.NET 10 SDK** y **Node.js 20+**. Los tests del backend pueden
  correrse sin SDK local (ver más abajo).

## Arranque rápido

Desde la raíz del repositorio:

```bash
docker compose up --build
```

Una vez levantado:

| Servicio   | URL                                            |
|------------|------------------------------------------------|
| Frontend   | http://localhost:3000                          |
| Backend    | http://localhost:5080/api/sales                |
| Mock       | http://localhost:8000/sales.txt                |

Para parar: `Ctrl+C`, o `docker compose down` para eliminar los contenedores.

## API

Base: `http://localhost:5080`

| Método | Endpoint                  | Respuesta                                                   |
|--------|---------------------------|-------------------------------------------------------------|
| `GET`  | `/api/sales`              | Listado de ventas (`Sale[]`).                               |
| `GET`  | `/api/sales/by-product`   | Totales por producto (`{ product, totalAmount }[]`), desc.  |
| `GET`  | `/api/sales/by-customer`  | Totales por cliente (`{ customerId, totalAmount }[]`), desc.|

Ejemplo:

```bash
curl -s http://localhost:5080/api/sales/by-product
```

Los errores esperables (origen no disponible, datos malformados) se devuelven como **ProblemDetails**
(RFC 7807) con el status adecuado: `404` (NotFound), `400` (Validation), `502` (Unavailable),
`500` (Unexpected).

## Desarrollo local

### Backend (.NET 10)

```bash
cd backend
dotnet run                       # arranca en http://localhost:5080
dotnet build                     # compila
```

Configuración por variables de entorno / `appsettings`:

- `SapMock__BaseUrl` — URL del mock (en Docker: `http://sap-mock:8080`; en local por defecto el mismo).
- `Cors__AllowedOrigins__0` — orígenes permitidos para el navegador (por defecto `http://localhost:3000`).
  **Nunca** ampliar a `AllowAnyOrigin`.

### Frontend (Next.js)

```bash
cd frontend
npm install
npm run dev                      # http://localhost:3000
npm run build                    # build de producción
npm run lint                     # eslint
```

- `BACKEND_URL` — URL del backend (en Docker: `http://backend:8080`; en local por defecto `http://localhost:5080`).

### Mock (nginx)

Edita los ficheros de `sap-mock/data/` y reconstruye la imagen (`docker compose up --build sap-mock`).

## Tests

**Backend** (vía SDK .NET dentro de un contenedor, **no requiere SDK local**, solo Docker):

```bash
./scripts/test-backend.sh                                   # toda la suite
./scripts/test-backend.sh --filter FullyQualifiedName~ResultTests   # filtrado
```

Si tienes el **.NET 10 SDK** instalado, también puedes correrlos directamente:

```bash
cd backend && dotnet test tests/SapAnalytics.Tests/SapAnalytics.Tests.csproj
```

**Frontend**:

```bash
cd frontend
npm run test:run                 # una pasada (CI)
npm run test                     # modo watch
```

## Estructura del proyecto

```
.
├── backend/                     # API .NET 10 (arquitectura hexagonal)
│   ├── Domain/                  #   núcleo puro: Sale, Result, Error, read models
│   ├── Application/             #   casos de uso (SalesAnalytics)
│   │   └── Ports/               #     contratos (ISalesRepository)
│   ├── Infrastructure/
│   │   ├── Inbound/Http/        #   adaptador de entrada: controladores + Error→HTTP
│   │   └── Outbound/MockTxt/    #   adaptador de salida: lectura del mock
│   ├── Program.cs               #   composición / DI
│   └── tests/                   #   xUnit (espejo de la estructura de src)
├── frontend/                    # Next.js (App Router) + Recharts
│   └── app/                     #   page.tsx (Server Component) + components/
├── sap-mock/                    # nginx que sirve data/sales.txt
│   └── data/                    #   fixtures ficticias (commiteadas)
├── scripts/test-backend.sh      # runner de tests del backend (dockerizado)
├── docker-compose.yml           # orquestación de las tres piezas
├── CLAUDE.md                    # guía para Claude Code (+ CLAUDE.md por pieza)
├── plan-proyecto-sap.md         # plan detallado por fases
└── DEUDA-TECNICA.md             # registro de deuda técnica
```

## Convenciones de datos (estilo SAP)

El mock imita un export real de SAP, así que sus ficheros siguen sus rarezas:

- **Delimitados por barra vertical** (`|`). Primera línea = cabecera.
  Columnas: `DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT`.
- **Codificación Latin-1 (ISO-8859-1)**, NO UTF-8. Si los acentos o la `ñ` salen mal al leer, casi seguro
  es esto. El adaptador del backend lee con ISO-8859-1.
- **Fechas** en `YYYYMMDD`, sin separadores.

## Datos y seguridad

- **Nunca se commitean secretos** (`.env`, credenciales, tokens) ni **datos reales** de SAP.
- Los datos reales van en `sap-mock/data-real/` o `sap-mock/private/` (ambos ignorados por git) o fuera del repo.
- Los `.txt` de `sap-mock/data/` son **fixtures totalmente ficticias** y sí se commitean.

## Documentación adicional

- [`CLAUDE.md`](./CLAUDE.md) — guía de trabajo (reglas globales; cada pieza tiene además su propio `CLAUDE.md`).
- [`plan-proyecto-sap.md`](./plan-proyecto-sap.md) — plan por fases (mock → backend → frontend →
  dockerización → persistencia opcional → SAP real → seguridad y despliegue).
- [`DEUDA-TECNICA.md`](./DEUDA-TECNICA.md) — registro de deuda técnica.
