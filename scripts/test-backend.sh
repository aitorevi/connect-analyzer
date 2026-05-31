#!/usr/bin/env bash
set -euo pipefail

# Run the backend test suite using the .NET 10 SDK inside a one-off container.
# Requires Docker; no local .NET 10 SDK needed.
# Extra args are forwarded to `dotnet test` (e.g. --filter, -v normal).

cd "$(dirname "$0")/.."

docker run --rm \
  -v "$(pwd)/backend:/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/ConnectAnalyzer.Tests/ConnectAnalyzer.Tests.csproj "$@"
