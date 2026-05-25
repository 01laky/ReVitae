#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

npx --yes markdownlint-cli2 "**/*.md" "#node_modules" "#prompts/021-multi-format-cv-import.md"
