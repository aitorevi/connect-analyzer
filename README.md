# Connect Analyzer

[![CI](https://github.com/aitorevi/connect-analyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/aitorevi/connect-analyzer/actions/workflows/ci.yml)

🇪🇸 Español · [🇬🇧 English](./README.en.md)

Herramienta interna para **analizar grandes cantidades de datos provenientes de SAP** y visualizarlos
en un frontal con gráficos. Empieza a pequeña escala con datos simulados y se itera poco a poco: el
objetivo es tener pronto el flujo más fino de punta a punta (**un fichero `.txt` → un endpoint → un
gráfico**) y a partir de ahí engordar con más análisis, filtros y, solo después, persistencia y origen
real de SAP.

## Demo en vivo

| Pieza        | Hosting                | URL                                          |
|--------------|------------------------|----------------------------------------------|
| Dashboard    | Vercel                 | <https://connect-analyzer.vercel.app>        |
| Backend API  | Google Cloud Run       | <https://connect-analyzer-api-370913301749.europe-southwest1.run.app> |
| Mock SAP     | Google Cloud Run       | <https://connect-analyzer-mock-370913301749.europe-southwest1.run.app> |

El frontend (Vercel) hace fetch **server-side** del backend en **Cloud Run**, que lee del mock y
calcula la analítica; el dashboard deriva KPIs/series/filtros en cliente. Cloud Run arranca en ~1-2 s
con la petición esperando (sin el cold-start de "no hay datos" que daba Render). El frontend apunta
al backend con la env var `BACKEND_URL`.

- **Cómo está montada la demo** (front, datos, backend): ver [`DEMO.md`](./DEMO.md).
- **Caso de estudio**: [Post en aitorevi.dev](https://aitorevi.dev/blog/connect-analyzer) — por qué hexagonal,
  patrón `Result`/`Error`, adaptador SAP real y persistencia con SQLite.
- **Cómo desplegar el backend de cero**: ver [`DEPLOY.md`](./DEPLOY.md) (Google Cloud Run + Vercel).

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
| `backend/mocks/sap/` | nginx (unprivileged)        | `8000 → 8080`         | Origen de datos simulado. Sirve un `.txt` que imita un export de SAP. |
| `backend/`   | C# / **.NET 10** (Web API)          | `5080 → 8080`         | Lee de la fuente, persiste en SQLite y sirve REST al front. Arquitectura hexagonal. |
| `frontend/`  | **Next.js** (App Router) + Recharts | `3000`                | Consume la API y pinta gráficos.                                   |

> El backend usa **dos puertos outbound**:
> - **`ISalesRepository`** — fuente de datos (mock, SAP real o Shopify). Selector por config (`SalesSource`).
> - **`ISalesStore`** — almacén local (SQLite). El caso de uso `IngestSales` lee de la fuente y guarda
>   en el almacén; la analítica lee del almacén. Cambiar de mock a SAP real, a Shopify, o de SQLite a Postgres,
>   = **escribir un adaptador nuevo**, sin tocar dominio ni aplicación.

## Stack

- **Backend**: C# con **.NET 10** (Web API), arquitectura hexagonal (Ports & Adapters), manejo de errores
  esperables con un tipo `Result<T>`/`Error` propio. Tests con **xUnit**.
- **Persistencia**: **SQLite** vía `Microsoft.Data.Sqlite` con SQL a mano (sin ORM).
- **Fuente real**: adaptador OData contra el sandbox del [SAP Business Accelerator Hub](https://api.sap.com).
- **Frontend**: **Next.js 16** + **TypeScript** (App Router) + **Recharts**. Tests con **Vitest** + React Testing Library.
- **Mock**: **nginx** sirviendo ficheros estáticos.
- **Orquestación**: **Docker** + **Docker Compose** (local), **Google Cloud Run** (backend + mock) + **Vercel** (demo en vivo).

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

Base local: `http://localhost:5080` · Producción: backend en Google Cloud Run (ver [`DEPLOY.md`](./DEPLOY.md)).

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
(RFC 7807) con el status adecuado: `404` (NotFound), `400` (Validation), `401` (Unauthorized),
`502` (Unavailable), `500` (Unexpected).

## Desarrollo local

### Backend (.NET 10)

```bash
cd backend
dotnet run                       # arranca en http://localhost:5080
dotnet build                     # compila
```

Configuración por variables de entorno / `appsettings` (ver también [`.env.example`](./.env.example)):

- `SalesSource` — `Mock` (por defecto), `Sap` o `Shopify`. Selecciona qué adaptador de `ISalesRepository` se cablea.
- `Sap__ApiKey` — **secreto**, solo si `SalesSource=Sap`. API key del Business Accelerator Hub.
  Localmente: `dotnet user-secrets set "Sap:ApiKey" "<tu-key>"`.
- `Sap__BaseUrl` — URL base del OData de SAP (por defecto el sandbox de `API_SALES_ORDER_SRV`).
- `SapMock__BaseUrl` — URL del mock (en Docker: `http://sap-mock:8080`).
- `Shopify__StoreUrl` — solo si `SalesSource=Shopify`. URL de la dev store (p.ej.
  `https://mi-tienda.myshopify.com`).
- `Shopify__ClientId` — solo si `SalesSource=Shopify`. ID de cliente de la app del Dev Dashboard.
- `Shopify__ClientSecret` — **secreto**, solo si `SalesSource=Shopify`. Se intercambia por un access
  token vía Client Credentials Grant. Localmente: `dotnet user-secrets set "Shopify:ClientSecret" "<tu-secret>"`.
- `Shopify__ApiVersion` — versión de la Admin API (por defecto `2025-01`).
- `Sqlite__Path` — ruta del fichero SQLite (por defecto `sales.db`; en Cloud Run y en Docker Compose
  usamos `/tmp/sales.db` porque el backend corre como usuario no-root).
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
  `http://localhost:5080`; en Vercel, la URL del backend en Cloud Run). El frontend lee de aquí en
  SSR; si el backend no responde, se muestra el error boundary. Ver [`DEMO.md`](./DEMO.md).

### Mock (nginx)

Edita los ficheros de `backend/mocks/sap/data/` y reconstruye la imagen (`docker compose up --build sap-mock`).

## Tests

**Backend** (vía SDK .NET dentro de un contenedor, **no requiere SDK local**, solo Docker):

```bash
./scripts/test-backend.sh                                          # toda la suite
./scripts/test-backend.sh --filter FullyQualifiedName~ResultTests  # filtrado
```

Si tienes el **.NET 10 SDK** instalado, también puedes correrlos directamente:

```bash
cd backend && dotnet test tests/ConnectAnalyzer.Tests/ConnectAnalyzer.Tests.csproj
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
│   │       ├── Shopify/                  #     adaptador Shopify Admin REST (OAuth Client Credentials)
│   │       └── Sqlite/                   #     adaptador SQLite (SQL a mano)
│   ├── Program.cs                        #   composición / DI + seed con retry
│   ├── tests/                            #   xUnit (espejo de la estructura de src)
│   └── mocks/sap/                        #   mock de SAP: nginx que sirve data/sales.txt (fixtures)
├── frontend/                             # Next.js (App Router) + Recharts
│   └── app/                              #   page.tsx (Server Component) + components/
├── scripts/test-backend.sh               # runner de tests del backend (dockerizado)
├── scripts/deploy-cloudrun.sh            # despliegue de backend + mock en Cloud Run
├── docker-compose.yml                    # orquestación local de las tres piezas
├── .github/workflows/ci.yml              # CI en GitHub Actions
├── DEPLOY.md                             # cómo desplegar la demo (Cloud Run + Vercel)
├── .env.example                          # variables de entorno documentadas
├── CLAUDE.md                             # guía para Claude Code (+ CLAUDE.md por pieza)
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
- Los datos reales van en `backend/mocks/sap/data-real/` o `.../private/` (ambos ignorados por git) o fuera del repo.
- Los `.txt` de `backend/mocks/sap/data/` son **fixtures totalmente ficticias** y sí se commitean.

## Documentación adicional

- [`DEPLOY.md`](./DEPLOY.md) — cómo desplegar la demo en vivo (Google Cloud Run + Vercel).
- [Post en aitorevi.dev](https://aitorevi.dev/blog/connect-analyzer) — caso de estudio: por qué hexagonal,
  el patrón `Result`/`Error`, el adaptador SAP real y la persistencia con SQLite.
- [`CLAUDE.md`](./CLAUDE.md) — guía de trabajo (reglas globales; cada pieza tiene su propio `CLAUDE.md`).
- [`DEUDA-TECNICA.md`](./DEUDA-TECNICA.md) — registro de deuda técnica.
- [`README.en.md`](./README.en.md) — English version.
