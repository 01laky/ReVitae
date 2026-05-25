#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

dotnet restore ReVitae.sln
dotnet format ReVitae.sln --verify-no-changes --include src/ReVitae.Core/ --include tests/ReVitae.Tests/ --verbosity minimal
dotnet build tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-restore
dotnet test tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-build
