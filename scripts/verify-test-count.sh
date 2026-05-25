#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

BASELINE="$(grep -E 'public const int MinimumTestCount = [0-9]+;' tests/ReVitae.Tests/TestCountBaselineTests.cs \
  | sed -E 's/.*MinimumTestCount = ([0-9]+);/\1/')"

if [[ -z "$BASELINE" ]]; then
  echo "Could not read MinimumTestCount from TestCountBaselineTests.cs" >&2
  exit 1
fi

README_COUNT="$(grep -oE 'tests-[0-9]+ passing' README.md | head -1 | sed -E 's/tests-([0-9]+) passing/\1/')"
if [[ -z "$README_COUNT" ]]; then
  echo "Could not read test count from README.md badge" >&2
  exit 1
fi

if [[ "$README_COUNT" != "$BASELINE" ]]; then
  echo "README test badge ($README_COUNT) does not match TestCountBaselineTests ($BASELINE)" >&2
  exit 1
fi

echo "Running dotnet test to verify count >= $BASELINE ..."
OUTPUT="$(dotnet test tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-restore -v q 2>&1 || true)"
echo "$OUTPUT"

ACTUAL="$(echo "$OUTPUT" | grep -oE 'Total: +[0-9]+' | tail -1 | tr -d ' ' | cut -d: -f2)"
if [[ -z "$ACTUAL" ]]; then
  echo "Could not parse dotnet test Total count" >&2
  exit 1
fi

if [[ "$ACTUAL" -lt "$BASELINE" ]]; then
  echo "Actual test count ($ACTUAL) is below baseline ($BASELINE)" >&2
  exit 1
fi

echo "Test count OK: actual=$ACTUAL baseline=$BASELINE readme=$README_COUNT"
