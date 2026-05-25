---
name: add-analytics-endpoint
description: Checklist to add a new sales-analytics endpoint to the .NET 10 backend following the hexagonal flow (port to application to controller to error mapping) plus mirrored tests. Use when asked to add a new analytics endpoint, aggregation, or report to the backend.
disable-model-invocation: true
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
---

# Add an analytics endpoint

Follow the hexagonal flow and the `Result`/`Error` discipline from `backend/CLAUDE.md`. Keep the
data-source boundary sacred: only the outbound adapter behind `ISalesRepository` knows the source.

## Flow (respect the layers)

1. **Domain** — if the endpoint returns a new shape, add an immutable `record` in `backend/Domain/`
   (e.g. `ProductTotal`, `CustomerTotal`). English identifiers. No framework/HTTP knowledge.
2. **Port** — only touch `backend/Application/Ports/ISalesRepository.cs` if you genuinely need new data
   from the source (e.g. a `SalesFilter`). Otherwise reuse `SearchAsync`. New filters return
   `Result` (e.g. "no match" → `Error.NotFound(...)`) — do not throw.
3. **Application** — add the use case in `backend/Application/SalesAnalytics.cs`. Chain with `Map`
   (pure transform) / `Bind` (step returning `Result`). **Never unwrap** the `Result`; no `try/catch`.
4. **Controller (inbound)** — add the action in `backend/Infrastructure/Inbound/Http/SalesController.cs`.
   This is the **only** place that opens the `Result`: `result.Match<IActionResult>(Ok, ErrorHttpResults.ToActionResult)`.
   Route stays English (`/api/sales/...`).
5. **Error mapping** — reuse `backend/Infrastructure/Inbound/Http/ErrorHttpResults.cs`. Only extend it if a
   new `ErrorType` is introduced (keep it the single Error→HTTP point).
6. **Adapter** — only change `MockTxtSalesRepository` if the source contract changed. Catch infrastructure
   exceptions at its edge → `Error.Unavailable`; re-throw `OperationCanceledException`.

## Mirrored tests (xUnit)

Add tests alongside the layers under `backend/tests/SapAnalytics.Tests/`:

- `Application/SalesAnalyticsTests.cs` — aggregation correctness + failure propagation, using
  `StubSalesRepository.Returning(...)` / `.Failing(error)` from `TestDoubles/`.
- `Api/SalesEndpointsTests.cs` — integration via `WebApplicationFactory<Program>` with a fake
  `ISalesRepository`; assert HTTP 200 + JSON shape, and the relevant error → status code.
- Naming `Method_Condition_Expected`; Arrange-Act-Assert.

## Verify

Run the suite with the `run-backend-tests` skill (`./scripts/test-backend.sh`). Optionally smoke-test
live with `docker compose up --build` then `curl -s http://localhost:5080/api/sales/<new-route>`.
