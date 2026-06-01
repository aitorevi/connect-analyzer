# Connect Analyzer

[![CI](https://github.com/aitorevi/connect-analyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/aitorevi/connect-analyzer/actions/workflows/ci.yml)

🇬🇧 English · [🇪🇸 Español](./README.md)

Internal tool to **analyse large amounts of data coming from SAP** and visualise it in a dashboard with
charts. It starts small with simulated data and grows iteratively: the first goal is the thinnest
end-to-end flow that works — **one `.txt` file → one endpoint → one chart** — and from there it grows with
more analyses, filters, and only later real persistence and a real SAP source.

## Live demo

| Piece        | URL                                                     | Hosting    |
|--------------|---------------------------------------------------------|------------|
| Dashboard    | <https://sap-analyzer.vercel.app>                       | Vercel     |
| Backend API  | <https://sap-analyzer-api.onrender.com>                 | Render     |
| Mock SAP     | <https://sap-analyzer-mock.onrender.com>                | Render     |

> Render's Free tier sleeps services after ~15 min without traffic, so the **first request after that
> can take ~30-50 s** (cold start). Subsequent loads are instant. The backend retries the seed-on-startup
> with backoff so it self-heals even while the mock is also waking up.

- **Case study**: [Post on aitorevi.dev](https://aitorevi.dev/en/blog/sap-analyzer) — why hexagonal,
  the `Result`/`Error` pattern, the real SAP adapter and the SQLite persistence layer.
- **Deploy from scratch**: see [`DEPLOY.md`](./DEPLOY.md) (Render Blueprint + Vercel, no credit card).

## Architecture

Three pieces, each in its own folder, orchestrated with **Docker Compose** locally:

```
┌────────────┐      HTTP       ┌────────────┐      HTTP/JSON   ┌────────────┐
│  frontend  │ ───────────────▶│  backend   │ ───────────────▶│  sap-mock  │
│  Next.js   │  /api/sales/... │  .NET 10   │   /sales.txt    │   nginx    │
│  :3000     │◀─────────────── │  :5080→8080│◀─────────────── │  :8000→8080│
└────────────┘                 └────────────┘                 └────────────┘
   charts                    API + analytics + SQLite        simulated SAP export
```

| Piece        | Tech                                | Port (host→internal)  | Role                                                                |
|--------------|-------------------------------------|-----------------------|---------------------------------------------------------------------|
| `sap-mock/`  | nginx (unprivileged)                | `8000 → 8080`         | Simulated data source. Serves a `.txt` mimicking an SAP export.     |
| `backend/`   | C# / **.NET 10** (Web API)          | `5080 → 8080`         | Reads from the source, persists in SQLite, and serves REST. Hexagonal architecture. |
| `frontend/`  | **Next.js** (App Router) + Recharts | `3000`                | Consumes the API and renders charts.                                |

> The backend has **two outbound ports**:
> - **`ISalesRepository`** — data source (mock or real SAP). Selected by config (`SalesSource`).
> - **`ISalesStore`** — local store (SQLite). The `IngestSales` use case reads from the source and
>   writes to the store; analytics read from the store. Switching from mock to real SAP, or from
>   SQLite to Postgres, = **writing a new adapter**, without touching the domain or application.

## Stack

- **Backend**: C# with **.NET 10** (Web API), hexagonal architecture (Ports & Adapters), expected-error
  handling with an in-house `Result<T>`/`Error` type. Tests with **xUnit**.
- **Persistence**: **SQLite** via `Microsoft.Data.Sqlite` with hand-written SQL (no ORM).
- **Real source**: OData adapter against the [SAP Business Accelerator Hub](https://api.sap.com) sandbox.
- **Frontend**: **Next.js 16** + **TypeScript** (App Router) + **Recharts**. Tests with **Vitest** +
  React Testing Library.
- **Mock**: **nginx** serving static files.
- **Orchestration**: **Docker** + **Docker Compose** (local), **Render** + **Vercel** (live demo).

## Prerequisites

- **Docker** and **Docker Compose** (the only hard requirement to bring everything up locally).
- *Optional, for native development only*: **.NET 10 SDK** and **Node.js 20+**. Backend tests can be run
  without a local SDK (see below).

## Quickstart

From the repository root:

```bash
docker compose up --build
```

Once running:

| Service    | URL                                            |
|------------|------------------------------------------------|
| Frontend   | http://localhost:3000                          |
| Backend    | http://localhost:5080/api/sales                |
| Mock       | http://localhost:8000/sales.txt                |

To stop: `Ctrl+C`, or `docker compose down` to remove the containers.

## API

Local base: `http://localhost:5080` · Production: `https://sap-analyzer-api.onrender.com`

| Method | Endpoint                  | Response                                                                        |
|--------|---------------------------|---------------------------------------------------------------------------------|
| `GET`  | `/api/sales`              | Sales list from the store (`Sale[]`).                                           |
| `GET`  | `/api/sales/by-product`   | Totals by product (`{ product, totalAmount }[]`), descending.                   |
| `GET`  | `/api/sales/by-customer`  | Totals by customer (`{ customerId, totalAmount }[]`), descending.               |
| `POST` | `/api/sales/refresh`      | Triggers an ingest: reads from the source and replaces the store. Returns `{ ingested: number }`. |

Example:

```bash
curl -s http://localhost:5080/api/sales/by-product
curl -X POST http://localhost:5080/api/sales/refresh
```

Expected errors (source unavailable, malformed data) are returned as **ProblemDetails** (RFC 7807) with
the right status: `404` (NotFound), `400` (Validation), `502` (Unavailable), `500` (Unexpected).

## Local development

### Backend (.NET 10)

```bash
cd backend
dotnet run                       # listens on http://localhost:5080
dotnet build                     # compile
```

Configuration via environment variables / `appsettings` (see also [`.env.example`](./.env.example)):

- `SalesSource` — `Mock` (default) or `Sap`. Chooses which `ISalesRepository` adapter is wired.
- `Sap__ApiKey` — **secret**, required only if `SalesSource=Sap`. Business Accelerator Hub API key.
  Locally: `dotnet user-secrets set "Sap:ApiKey" "<your-key>"`.
- `Sap__BaseUrl` — base URL of the SAP OData service (defaults to the `API_SALES_ORDER_SRV` sandbox).
- `SapMock__BaseUrl` — mock URL (in Docker: `http://sap-mock:8080`).
- `Sqlite__Path` — SQLite file path (defaults to `sales.db`; on Render we use `/tmp/sales.db`).
- `Cors__AllowedOrigins__0` — origins allowed in the browser (defaults to `http://localhost:3000`).
  **Never** widen to `AllowAnyOrigin`.

### Frontend (Next.js)

```bash
cd frontend
npm install
npm run dev                      # http://localhost:3000
npm run build                    # production build
npm run lint                     # eslint
```

- `BACKEND_URL` — backend URL (in Docker: `http://backend:8080`; default `http://localhost:5080`;
  on Vercel it points to `https://sap-analyzer-api.onrender.com`).

### Mock (nginx)

Edit the files in `sap-mock/data/` and rebuild the image (`docker compose up --build sap-mock`).

## Tests

**Backend** (via .NET SDK inside a container, **no local SDK required**, only Docker):

```bash
./scripts/test-backend.sh                                          # whole suite
./scripts/test-backend.sh --filter FullyQualifiedName~ResultTests  # filtered
```

If you have the **.NET 10 SDK** installed, you can also run them directly:

```bash
cd backend && dotnet test tests/ConnectAnalyzer.Tests/ConnectAnalyzer.Tests.csproj
```

**Frontend**:

```bash
cd frontend
npm run test:run                 # one-shot (CI)
npm run test                     # watch mode
```

**CI**: every push and PR to `main` runs both jobs in GitHub Actions (see the badge at the top).

## Project structure

```
.
├── backend/                              # .NET 10 API (hexagonal architecture)
│   ├── Domain/                           #   pure core: Sale, Result, Error, read models
│   ├── Application/                      #   use cases: SalesAnalytics, IngestSales
│   │   └── Ports/                        #     contracts: ISalesRepository, ISalesStore
│   ├── Infrastructure/
│   │   ├── Inbound/Http/                 #     inbound adapter: controllers + Error→HTTP
│   │   └── Outbound/
│   │       ├── MockTxt/                  #     mock adapter (.txt Latin-1)
│   │       ├── Sap/                      #     real SAP OData adapter
│   │       └── Sqlite/                   #     SQLite adapter (hand-written SQL)
│   ├── Program.cs                        #   composition / DI + retrying seed
│   └── tests/                            #   xUnit (mirrors src structure)
├── frontend/                             # Next.js (App Router) + Recharts
│   └── app/                              #   page.tsx (Server Component) + components/
├── sap-mock/                             # nginx serving data/sales.txt
│   └── data/                             #   fictitious fixtures (committed)
├── scripts/test-backend.sh               # backend test runner (dockerised)
├── docker-compose.yml                    # local orchestration of the three pieces
├── render.yaml                           # Render Blueprint (backend + mock)
├── .github/workflows/ci.yml              # CI on GitHub Actions
├── DEPLOY.md                             # how to deploy the live demo (Render + Vercel)
├── .env.example                          # documented environment variables
├── CLAUDE.md                             # Claude Code guidance (+ a CLAUDE.md per piece)
├── plan-proyecto-sap.md                  # detailed phase-by-phase plan
└── DEUDA-TECNICA.md                      # technical-debt log
```

## Data conventions (SAP style)

The mock mimics a real SAP export, so its files follow its quirks:

- **Pipe-delimited** (`|`). First line = header.
  Columns: `DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT`.
- **Latin-1 (ISO-8859-1) encoding**, NOT UTF-8. If accents or `ñ` come out wrong on read, this is almost
  certainly the cause. The backend adapter reads with ISO-8859-1.
- **Dates** in `YYYYMMDD`, no separators.

## Data and security

- **Never commit secrets** (`.env`, credentials, tokens, **`Sap__ApiKey`**) or **real SAP data**.
- Real data goes in `sap-mock/data-real/` or `sap-mock/private/` (both gitignored) or outside the repo.
- The `.txt` files in `sap-mock/data/` are **purely fictitious fixtures** and are committed.

## Further reading

- [`DEPLOY.md`](./DEPLOY.md) — how to deploy the live demo (Render Blueprint + Vercel).
- [Blog post on aitorevi.dev](https://aitorevi.dev/en/blog/sap-analyzer) — case study: why hexagonal,
  the `Result`/`Error` pattern, the real SAP adapter and the SQLite persistence layer.
- [`CLAUDE.md`](./CLAUDE.md) — working guide (global rules; each piece has its own `CLAUDE.md`).
- [`plan-proyecto-sap.md`](./plan-proyecto-sap.md) — phased plan (mock → backend → frontend →
  dockerisation → optional persistence → real SAP → security and deployment).
- [`DEUDA-TECNICA.md`](./DEUDA-TECNICA.md) — technical-debt log.
