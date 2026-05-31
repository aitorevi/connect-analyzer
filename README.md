# SAP Analyzer

[![CI](https://github.com/aitorevi/sap-analyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/aitorevi/sap-analyzer/actions/workflows/ci.yml)

🇪🇸 Español · [🇬🇧 English](./README.en.md)

Herramienta interna para **analizar grandes cantidades de datos provenientes de SAP** y visualizarlos
en un frontal con gráficos. Empieza a pequeña escala con datos simulados y se itera poco a poco: el
objetivo es tener pronto el flujo más fino de punta a punta (**un fichero `.txt` → un endpoint → un
gráfico**) y a partir de ahí engordar con más análisis, filtros y, solo después, persistencia y origen
real de SAP.

## Demo en vivo

| Pieza        | URL                                                     | Hosting    |
|--------------|---------------------------------------------------------|------------|
| Dashboard    | <https://sap-analyzer.vercel.app>                       | Vercel     |
| Backend API  | <https://sap-analyzer-api.onrender.com>                 | Render     |
| Mock SAP     | <https://sap-analyzer-mock.onrender.com>                | Render     |

> El tier gratuito de Render duerme los servicios tras ~15 min sin tráfico, así que la **primera
> carga puede tardar ~30-50 s** (cold-start). Recargar es instantáneo. El backend reintenta el
> seed-on-startup con backoff para autocurarse cuando el mock también está despertando.

- **Caso de estudio**: [Post en aitorevi.dev](https://aitorevi.dev/blog/sap-analyzer) — por qué hexagonal,
  patrón `Result`/`Error`, adaptador SAP real y persistencia con SQLite.
- **Cómo desplegarlo de cero**: ver [`DEPLOY.md`](./DEPLOY.md) (Render Blueprint + Vercel, sin tarjeta).

## Arquitectura

Tres piezas, cada una en su carpeta, orquestadas con **Docker Compose** en local:

```
┌────────────┐      HTTP       ┌────────────┐      HTTP/JSON   ┌────────────┐
│  frontend  │ ───────────────▶│  backend   │ ───────────────▶│  sap-mock  │
│  Next.js   │  /api/sales/... │  .NET 10   │   /sales.txt    │   nginx    │
│  :3000     │◀─────────────── │  :5080→8080│◀─────────────── │  :8000→8080│
└────────────┘                 └────────────┘                 └────────────┘
   gráficos                  API + análisis + SQLite        export simulado de SAP
```

| Pieza        | Tecnología                          | Puerto (host→interno) | Rol                                                                |
|--------------|-------------------------------------|-----------------------|--------------------------------------------------------------------|
| `sap-mock/`  | nginx (unprivileged)                | `8000 → 8080`         | Origen de datos simulado. Sirve un `.txt` que imita un export de SAP. |
| `backend/`   | C# / **.NET 10** (Web API)          | `5080 → 8080`         | Lee de la fuente, persiste en SQLite y sirve REST al front. Arquitectura hexagonal. |
| `frontend/`  | **Next.js** (App Router) + Recharts | `3000`                | Consume la API y pinta gráficos.                                   |

> El backend usa **dos puertos outbound**:
> - **`ISalesRepository`** — fuente de datos (mock o SAP real). Selector por config (`SalesSource`).
> - **`ISalesStore`** — almacén local (SQLite). El caso de uso `IngestSales` lee de la fuente y guarda
>   en el almacén; la analítica lee del almacén. Cambiar de mock a SAP real, o de SQLite a Postgres,
>   = **escribir un adaptador nuevo**, sin tocar dominio ni aplicación.

## Stack

- **Backend**: C# con **.NET 10** (Web API), arquitectura hexagonal (Ports & Adapters), manejo de errores
  esperables con un tipo `Result<T>`/`Error` propio. Tests con **xUnit**.
- **Persistencia**: **SQLite** vía `Microsoft.Data.Sqlite` con SQL a mano (sin ORM).
- **Fuente real**: adaptador OData contra el sandbox del [SAP Business Accelerator Hub](https://api.sap.com).
- **Frontend**: **Next.js 16** + **TypeScript** (App Router) + **Recharts**. Tests con **Vitest** + React Testing Library.
- **Mock**: **nginx** sirviendo ficheros estáticos.
- **Orquestación**: **Docker** + **Docker Compose** (local), **Render** + **Vercel** (demo en vivo).

## Requisitos previos

- **Docker** y **Docker Compose** (única dependencia obligatoria para levantar todo en local).
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

Base local: `http://localhost:5080` · Producción: `https://sap-analyzer-api.onrender.com`

| Método | Endpoint                  | Respuesta                                                                       |
|--------|---------------------------|---------------------------------------------------------------------------------|
| `GET`  | `/api/sales`              | Listado de ventas del almacén (`Sale[]`).                                       |
| `GET`  | `/api/sales/by-product`   | Totales por producto (`{ product, totalAmount }[]`), descendente.               |
| `GET`  | `/api/sales/by-customer`  | Totales por cliente (`{ customerId, totalAmount }[]`), descendente.             |
| `POST` | `/api/sales/refresh`      | Dispara una ingesta: lee de la fuente y reemplaza el almacén. Devuelve `{ ingested: number }`. |

Ejemplo:

```bash
curl -s http://localhost:5080/api/sales/by-product
curl -X POST http://localhost:5080/api/sales/refresh
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

Configuración por variables de entorno / `appsettings` (ver también [`.env.example`](./.env.example)):

- `SalesSource` — `Mock` (por defecto) o `Sap`. Selecciona qué adaptador de `ISalesRepository` se cablea.
- `Sap__ApiKey` — **secreto**, solo si `SalesSource=Sap`. API key del Business Accelerator Hub.
  Localmente: `dotnet user-secrets set "Sap:ApiKey" "<tu-key>"`.
- `Sap__BaseUrl` — URL base del OData de SAP (por defecto el sandbox de `API_SALES_ORDER_SRV`).
- `SapMock__BaseUrl` — URL del mock (en Docker: `http://sap-mock:8080`).
- `Sqlite__Path` — ruta del fichero SQLite (por defecto `sales.db`; en Render usamos `/tmp/sales.db`).
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

- `BACKEND_URL` — URL del backend (en Docker: `http://backend:8080`; en local por defecto
  `http://localhost:5080`; en Vercel apunta a `https://sap-analyzer-api.onrender.com`).

### Mock (nginx)

Edita los ficheros de `sap-mock/data/` y reconstruye la imagen (`docker compose up --build sap-mock`).

## Tests

**Backend** (vía SDK .NET dentro de un contenedor, **no requiere SDK local**, solo Docker):

```bash
./scripts/test-backend.sh                                          # toda la suite
./scripts/test-backend.sh --filter FullyQualifiedName~ResultTests  # filtrado
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

**CI**: cada push y PR a `main` ejecuta ambos jobs en GitHub Actions (ver el badge arriba).

## Estructura del proyecto

```
.
├── backend/                              # API .NET 10 (arquitectura hexagonal)
│   ├── Domain/                           #   núcleo puro: Sale, Result, Error, read models
│   ├── Application/                      #   casos de uso: SalesAnalytics, IngestSales
│   │   └── Ports/                        #     contratos: ISalesRepository, ISalesStore
│   ├── Infrastructure/
│   │   ├── Inbound/Http/                 #     adaptador de entrada: controladores + Error→HTTP
│   │   └── Outbound/
│   │       ├── MockTxt/                  #     adaptador del mock (.txt Latin-1)
│   │       ├── Sap/                      #     adaptador OData del SAP real
│   │       └── Sqlite/                   #     adaptador SQLite (SQL a mano)
│   ├── Program.cs                        #   composición / DI + seed con retry
│   └── tests/                            #   xUnit (espejo de la estructura de src)
├── frontend/                             # Next.js (App Router) + Recharts
│   └── app/                              #   page.tsx (Server Component) + components/
├── sap-mock/                             # nginx que sirve data/sales.txt
│   └── data/                             #   fixtures ficticias (commiteadas)
├── scripts/test-backend.sh               # runner de tests del backend (dockerizado)
├── docker-compose.yml                    # orquestación local de las tres piezas
├── render.yaml                           # Blueprint de Render (backend + mock)
├── .github/workflows/ci.yml              # CI en GitHub Actions
├── DEPLOY.md                             # cómo desplegar la demo (Render + Vercel)
├── .env.example                          # variables de entorno documentadas
├── CLAUDE.md                             # guía para Claude Code (+ CLAUDE.md por pieza)
├── plan-proyecto-sap.md                  # plan detallado por fases
└── DEUDA-TECNICA.md                      # registro de deuda técnica
```

## Convenciones de datos (estilo SAP)

El mock imita un export real de SAP, así que sus ficheros siguen sus rarezas:

- **Delimitados por barra vertical** (`|`). Primera línea = cabecera.
  Columnas: `DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT`.
- **Codificación Latin-1 (ISO-8859-1)**, NO UTF-8. Si los acentos o la `ñ` salen mal al leer, casi seguro
  es esto. El adaptador del backend lee con ISO-8859-1.
- **Fechas** en `YYYYMMDD`, sin separadores.

## Datos y seguridad

- **Nunca se commitean secretos** (`.env`, credenciales, tokens, **`Sap__ApiKey`**) ni **datos reales** de SAP.
- Los datos reales van en `sap-mock/data-real/` o `sap-mock/private/` (ambos ignorados por git) o fuera del repo.
- Los `.txt` de `sap-mock/data/` son **fixtures totalmente ficticias** y sí se commitean.

## Documentación adicional

- [`DEPLOY.md`](./DEPLOY.md) — cómo desplegar la demo en vivo (Render Blueprint + Vercel).
- [Post en aitorevi.dev](https://aitorevi.dev/blog/sap-analyzer) — caso de estudio: por qué hexagonal,
  el patrón `Result`/`Error`, el adaptador SAP real y la persistencia con SQLite.
- [`CLAUDE.md`](./CLAUDE.md) — guía de trabajo (reglas globales; cada pieza tiene su propio `CLAUDE.md`).
- [`plan-proyecto-sap.md`](./plan-proyecto-sap.md) — plan por fases (mock → backend → frontend →
  dockerización → persistencia opcional → SAP real → seguridad y despliegue).
- [`DEUDA-TECNICA.md`](./DEUDA-TECNICA.md) — registro de deuda técnica.
- [`README.en.md`](./README.en.md) — English version.
