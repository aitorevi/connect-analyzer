#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

docker run --rm \
  -v "$(pwd)/backend:/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/ConnectAnalyzer.Tests/ConnectAnalyzer.Tests.csproj "$@"
