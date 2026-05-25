#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

dotnet restore tests/ReVitae.Tests/ReVitae.Tests.csproj

# dotnet format --verify-no-changes treats CRLF checkouts on Windows as dirty even
# with core.autocrlf disabled; Ubuntu and macOS still enforce formatting here.
if [ "${RUNNER_OS:-local}" != "Windows" ]; then
	dotnet format src/ReVitae.Core/ReVitae.Core.csproj --verify-no-changes --verbosity minimal
	dotnet format src/ReVitae/ReVitae.csproj --verify-no-changes --verbosity minimal
	dotnet format tests/ReVitae.Tests/ReVitae.Tests.csproj --verify-no-changes --verbosity minimal
fi

dotnet build tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-restore
if [ -n "${CI:-}" ]; then
	# John Doe import matrix (51 variants) runs in the dedicated import-matrix job on Ubuntu.
	TEST_FILTER="Category!=ImportMatrix"
	if [ "${RUNNER_OS:-local}" = "Windows" ]; then
		# PdfPig geometry and native Tesseract differ on Windows; covered on Ubuntu CI.
		TEST_FILTER="${TEST_FILTER}&Category!=OcrIntegration&Category!=ImportPdfReimport"
	fi

	dotnet test tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-build --filter "$TEST_FILTER"
else
	dotnet test tests/ReVitae.Tests/ReVitae.Tests.csproj --configuration Release --no-build
fi
