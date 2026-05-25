#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

OUTPUT="$(dotnet list ReVitae.sln package --vulnerable --include-transitive 2>&1)"

if echo "$OUTPUT" | grep -q "has the following vulnerable packages"; then
	echo "Vulnerable NuGet packages detected:" >&2
	echo "$OUTPUT" >&2
	exit 1
fi

echo "No vulnerable NuGet packages detected."
