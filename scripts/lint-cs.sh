#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

dotnet restore ReVitae.sln
dotnet format src/ReVitae.Core/ReVitae.Core.csproj --verify-no-changes --verbosity minimal
dotnet format tests/ReVitae.Tests/ReVitae.Tests.csproj --verify-no-changes --verbosity minimal
dotnet build tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-restore
dotnet test tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-build
