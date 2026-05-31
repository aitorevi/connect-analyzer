---
name: run-backend-tests
description: Run the .NET 10 backend test suite via the dockerized dotnet test runner. Use when asked to run, execute, or check backend tests, or after editing backend code under backend/. Forwards extra args to dotnet test (e.g. a --filter).
allowed-tools: Bash
---

# Run backend tests

The backend test suite runs inside a one-off .NET 10 SDK container — **no local .NET SDK is needed**, only Docker. The wrapper script lives at `scripts/test-backend.sh`.

## How to run

From the repository root:

```bash
./scripts/test-backend.sh
```

Extra arguments are forwarded straight to `dotnet test`. Common cases:

- Run a single test class or method:
  ```bash
  ./scripts/test-backend.sh --filter FullyQualifiedName~ResultTests
  ```
- More verbose output:
  ```bash
  ./scripts/test-backend.sh -v normal
  ```

## What it does

Runs `dotnet test tests/ConnectAnalyzer.Tests/ConnectAnalyzer.Tests.csproj` (xUnit) against
`mcr.microsoft.com/dotnet/sdk:10.0`, mounting `backend/` into the container.

## After running

- Report pass/fail counts and surface any failing test names with their assertion output.
- If the failure is a Latin-1 / encoding issue or a `Result`/`Error` flow issue, check the conventions in `backend/CLAUDE.md` before proposing a fix.
