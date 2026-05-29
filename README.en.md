# SAP Analyzer

🇬🇧 English · [🇪🇸 Español](./README.md)

Internal tool to **analyse large amounts of data coming from SAP** and visualise it in a dashboard with
charts. It starts small with simulated data and grows iteratively: the first goal is the thinnest
end-to-end flow that works — **one `.txt` file → one endpoint → one chart** — and from there it grows with
more analyses, filters, and only later real persistence and a real SAP source.

## Architecture

Three pieces, each in its own folder, orchestrated with **Docker Compose**:

```
┌────────────┐      HTTP       ┌────────────┐      HTTP/JSON   ┌────────────┐
│  frontend  │ ───────────────▶│  backend   │ ───────────────▶│  sap-mock  │
│  Next.js   │  /api/sales/... │  .NET 10   │   /sales.txt    │   nginx    │
│  :3000     │◀─────────────── │  :5080→8080│◀─────────────── │  :8000→8080│
└────────────┘                 └────────────┘                 └────────────┘
   charts                    REST API + analytics             simulated SAP export
```

| Piece        | Tech                                | Port (host→internal)  | Role                                                                |
|--------------|-------------------------------------|-----------------------|---------------------------------------------------------------------|
| `sap-mock/`  | nginx (unprivileged)                | `8000 → 8080`         | Simulated data source. Serves a `.txt` mimicking an SAP export.     |
| `backend/`   | C# / **.NET 10** (Web API)          | `5080 → 8080`         | Reads from the source, processes and serves REST. Hexagonal arch.   |
| `frontend/`  | **Next.js** (App Router) + Recharts | `3000`                | Consumes the API and renders charts.                                |

> The data source is isolated behind the **`ISalesRepository` port** in the backend. Today it is
> implemented by an adapter that reads the mock (`MockTxtSalesRepository`); tomorrow it can be real SAP
> (OData, files, RFC/BAPI) by writing a new adapter, **without touching anything else**.

## Stack

- **Backend**: C# with **.NET 10** (Web API), hexagonal architecture (Ports & Adapters), expected-error
  handling with an in-house `Result<T>`/`Error` type. Tests with **xUnit**.
- **Frontend**: **Next.js 16** + **TypeScript** (App Router) + **Recharts**. Tests with **Vitest** +
  React Testing Library.
- **Mock**: **nginx** serving static files.
- **Orchestration**: **Docker** + **Docker Compose**.

## Prerequisites

- **Docker** and **Docker Compose** (the only hard requirement to bring everything up).
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

Base: `http://localhost:5080`

| Method | Endpoint                  | Response                                                    |
|--------|---------------------------|-------------------------------------------------------------|
| `GET`  | `/api/sales`              | Sales list (`Sale[]`).                                      |
| `GET`  | `/api/sales/by-product`   | Totals by product (`{ product, totalAmount }[]`), desc.     |
| `GET`  | `/api/sales/by-customer`  | Totals by customer (`{ customerId, totalAmount }[]`), desc. |

Example:

```bash
curl -s http://localhost:5080/api/sales/by-product
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

Configuration via environment variables / `appsettings`:

- `SapMock__BaseUrl` — mock URL (in Docker: `http://sap-mock:8080`; default the same locally).
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

- `BACKEND_URL` — backend URL (in Docker: `http://backend:8080`; default `http://localhost:5080`).

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
cd backend && dotnet test tests/SapAnalytics.Tests/SapAnalytics.Tests.csproj
```

**Frontend**:

```bash
cd frontend
npm run test:run                 # one-shot (CI)
npm run test                     # watch mode
```

## Project structure

```
.
├── backend/                     # .NET 10 API (hexagonal architecture)
│   ├── Domain/                  #   pure core: Sale, Result, Error, read models
│   ├── Application/             #   use cases (SalesAnalytics)
│   │   └── Ports/               #     contracts (ISalesRepository)
│   ├── Infrastructure/
│   │   ├── Inbound/Http/        #   inbound adapter: controllers + Error→HTTP
│   │   └── Outbound/MockTxt/    #   outbound adapter: reads from the mock
│   ├── Program.cs               #   composition / DI
│   └── tests/                   #   xUnit (mirrors src structure)
├── frontend/                    # Next.js (App Router) + Recharts
│   └── app/                     #   page.tsx (Server Component) + components/
├── sap-mock/                    # nginx serving data/sales.txt
│   └── data/                    #   fictitious fixtures (committed)
├── scripts/test-backend.sh      # backend test runner (dockerised)
├── docker-compose.yml           # orchestration of the three pieces
├── CLAUDE.md                    # Claude Code guidance (+ a CLAUDE.md per piece)
├── plan-proyecto-sap.md         # detailed phase-by-phase plan
└── DEUDA-TECNICA.md             # technical-debt log
```

## Data conventions (SAP style)

The mock mimics a real SAP export, so its files follow its quirks:

- **Pipe-delimited** (`|`). First line = header.
  Columns: `DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT`.
- **Latin-1 (ISO-8859-1) encoding**, NOT UTF-8. If accents or `ñ` come out wrong on read, this is almost
  certainly the cause. The backend adapter reads with ISO-8859-1.
- **Dates** in `YYYYMMDD`, no separators.

## Data and security

- **Never commit secrets** (`.env`, credentials, tokens) or **real SAP data**.
- Real data goes in `sap-mock/data-real/` or `sap-mock/private/` (both gitignored) or outside the repo.
- The `.txt` files in `sap-mock/data/` are **purely fictitious fixtures** and are committed.

## Further reading

- [`CLAUDE.md`](./CLAUDE.md) — working guide (global rules; each piece has its own `CLAUDE.md`).
- [`plan-proyecto-sap.md`](./plan-proyecto-sap.md) — phased plan (mock → backend → frontend →
  dockerisation → optional persistence → real SAP → security and deployment).
- [`DEUDA-TECNICA.md`](./DEUDA-TECNICA.md) — technical-debt log.
