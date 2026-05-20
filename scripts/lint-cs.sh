#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

dotnet restore ReVitae.sln
dotnet format ReVitae.sln --verify-no-changes --verbosity minimal
dotnet build ReVitae.sln --configuration Release --no-restore
