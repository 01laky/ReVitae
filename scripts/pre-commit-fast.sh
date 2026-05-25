#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

export REVITAE_FAST_PRECOMMIT=1
npm run lint:md
REVITAE_FAST_PRECOMMIT=1 ./scripts/lint-cs.sh
