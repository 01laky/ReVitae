#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

read_version_prop() {
  local property_name="$1"
  sed -n "s/.*<${property_name}>\([^<]*\)<\/${property_name}>.*/\1/p" Version.props | head -n 1
}

VERSION="$(read_version_prop VersionPrefix)"
if [[ -z "$VERSION" ]]; then
  VERSION="$(read_version_prop Version)"
fi

if [[ -z "$VERSION" ]]; then
  echo "Could not read app version from Version.props." >&2
  exit 1
fi

README_BADGE_VERSION="$(
  sed -n 's/.*badge\/app-\([^-]*\)-blue.*/\1/p' README.md | head -n 1
)"

if [[ "$README_BADGE_VERSION" != "$VERSION" ]]; then
  echo "README app badge version '$README_BADGE_VERSION' does not match Version.props '$VERSION'." >&2
  exit 1
fi

PACKAGE_JSON_VERSION="$(
  sed -n 's/^[[:space:]]*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' package.json | head -n 1
)"

if [[ "$PACKAGE_JSON_VERSION" != "$VERSION" ]]; then
  echo "package.json version '$PACKAGE_JSON_VERSION' does not match Version.props '$VERSION'." >&2
  exit 1
fi

ASSEMBLY_VERSION="$(read_version_prop AssemblyVersion)"
MANIFEST_VERSION="$(
  sed -n 's/.*assemblyIdentity version="\([^"]*\)".*/\1/p' src/ReVitae/app.manifest | head -n 1
)"

if [[ -n "$ASSEMBLY_VERSION" && -n "$MANIFEST_VERSION" && "$MANIFEST_VERSION" != "$ASSEMBLY_VERSION" ]]; then
  echo "app.manifest version '$MANIFEST_VERSION' does not match AssemblyVersion '$ASSEMBLY_VERSION'." >&2
  exit 1
fi

echo "Version consistency checks passed for $VERSION."
