# Prompt 020 - Application Versioning

Introduce a single, explicit versioning system for ReVitae so the product has one
real app version that is visible in the UI, embedded in builds, documented in
GitHub releases, and reflected in the README alongside the existing tech-stack
badges.

## Goal

ReVitae currently documents framework and dependency versions in the README, but
it does not expose a real **application version**. The desktop app, core library,
Windows manifest, npm metadata, and README can all drift apart because nothing
owns version information centrally.

This prompt adds a best-practice versioning layer for an early-stage desktop app:

1. one source of truth for the app version,
2. SemVer-based release numbering,
3. version metadata in .NET assemblies,
4. visible app version in the UI,
5. README badge for the app version,
6. release workflow guidance via Git tags and `CHANGELOG.md`,
7. tests and scripts that prevent silent version drift.

This prompt does **not** add auto-update, installers, or CI release automation yet.
It establishes the versioning foundation those features will depend on later.

## Current State

Today the repository has several unrelated version-like values:

| Location | Current value | Problem |
| --- | --- | --- |
| `package.json` | `1.0.0` | npm tooling only; not shown in app |
| `src/ReVitae/app.manifest` | `1.0.0.0` | hardcoded Windows assembly identity |
| `src/ReVitae/ReVitae.csproj` | none | no `Version` / `InformationalVersion` |
| `src/ReVitae.Core/ReVitae.Core.csproj` | none | no shared version metadata |
| README badges | `.NET 10.0`, `Avalonia 12.0`, tests count | no ReVitae app version badge |
| Setup modal | placeholder text only | no About/version section |
| Git tags / releases | none documented | no release convention |
| `CHANGELOG.md` | missing | no release history |

Important distinction:

- **Tech-stack badges** describe dependencies/platforms (`.NET`, `Avalonia`,
  QuestPDF, etc.).
- **App version badge** describes the ReVitae product release (`0.1.0`,
  `0.2.0`, etc.).

Both should coexist in the README. Do not replace stack badges with the app
version badge.

## Versioning Policy

Use [Semantic Versioning 2.0.0](https://semver.org/) for ReVitae.

Recommended scheme while the product is pre-1.0:

- `0.MINOR.PATCH`

Meaning:

- **MAJOR** stays `0` until the project declares a stable 1.0 product release.
- **MINOR** increases for user-visible feature increments (new sections, import
  improvements, export formats, persistence, etc.).
- **PATCH** increases for bug fixes, parser/export corrections, validation
  fixes, docs-only release notes corrections, and non-breaking internal refactors
  shipped as a maintenance release.

Pre-release labels are allowed before stable releases:

- `0.2.0-alpha.1`
- `0.2.0-beta.1`
- `0.2.0-rc.1`

Do **not** tie app version numbers directly to prompt numbers. Prompts are
implementation history; SemVer describes product releases.

### Initial version for this prompt

Set the first formal app version to:

```text
0.1.0
```

Reason:

- ReVitae already has substantial functionality, but it is still pre-1.0.
- This prompt creates the first intentional release baseline.
- Future releases can increment normally without rewriting history.

If release notes justify a higher first public baseline, `0.2.0` is acceptable,
but pick one value and document it in `CHANGELOG.md`. Do not leave the choice
ambiguous in code.

## Part 1 - Single Source of Truth

Create a repo-level MSBuild props file as the canonical app version source.

Preferred layout:

```text
Version.props
Directory.Build.props
```

### `Version.props`

Define at minimum:

- `VersionPrefix` or `Version` = SemVer base (`0.1.0`)
- optional `VersionSuffix` for pre-release labels (empty by default)

Recommended additional metadata:

- `AssemblyVersion` = `$(VersionMajor).$(VersionMinor).0.0`
- `FileVersion` = same as `Version` or include build metadata if needed
- `InformationalVersion` = full displayed version, optionally with Git commit
  suffix in local/dev builds

Example target behavior:

- Release build version: `0.1.0`
- Local dev build version: `0.1.0+dev` or `0.1.0+<short-sha>` if easy to add
  safely without breaking tests/CI

Keep v1 simple. A plain `0.1.0` everywhere is acceptable if Git suffix injection
adds too much complexity.

### `Directory.Build.props`

Import `Version.props` for all projects in the solution:

- `src/ReVitae/ReVitae.csproj`
- `src/ReVitae.Core/ReVitae.Core.csproj`
- `tests/ReVitae.Tests/ReVitae.Tests.csproj` should inherit metadata only if
  useful; at minimum the app and core libraries must share the same version.

Do not duplicate version numbers in individual `.csproj` files.

## Part 2 - .NET Assembly and Windows Metadata

Ensure the built app exposes version metadata consistently.

### Required assembly metadata

Both `ReVitae` and `ReVitae.Core` should expose:

- `AssemblyInformationalVersion` for human-readable app version,
- `AssemblyVersion` / `FileVersion` via MSBuild properties.

The UI must read the app version from the **desktop app assembly**, not from
hardcoded strings.

Suggested API in `ReVitae.Core`:

```csharp
public static class AppVersion
{
    public static string Current { get; }
    public static string Informational { get; }
    public static bool IsPreRelease { get; }
}
```

Implementation should use `Assembly.GetExecutingAssembly()` from the app layer
or a small wrapper passed in by the UI project. Avoid duplicating constants in
C# source.

### Windows manifest

Update `src/ReVitae/app.manifest` so `assemblyIdentity/@version` matches the
MSBuild version policy.

Preferred approach:

- generate/sync it from MSBuild during build, or
- document and test one manual mapping rule if generation is too heavy for v1.

Do not leave `1.0.0.0` hardcoded once real versioning exists.

## Part 3 - npm / Repository Metadata Sync

`package.json` currently contains `"version": "1.0.0"` for Node tooling only.

For this prompt:

- align `package.json` version with `Version.props`,
- treat it as repository metadata, not the runtime source of truth,
- optionally add a small verification script that fails when they diverge.

Do not move app runtime versioning to npm. .NET remains canonical.

## Part 4 - README and Release Documentation

### README badge

Add an app version badge near the existing stack badges, for example:

```markdown
[![App](https://img.shields.io/badge/app-0.1.0-blue)](https://github.com/01laky/ReVitae/releases)
```

Requirements:

- badge value must match `Version.props`,
- keep existing `.NET`, `Avalonia`, platform, and tests badges,
- update stale README values while touching the file (for example tests count),
- add a short README subsection explaining the difference between:
  - app version,
  - tech-stack versions,
  - dependency package versions in `.csproj`.

### `CHANGELOG.md`

Add a Keep a Changelog-style file:

- top section for `Unreleased`,
- first release section for `0.1.0`,
- summarize major capabilities already shipped before this prompt at a high level,
- document how future releases should be recorded.

Do not rewrite full project history prompt-by-prompt. Summarize product areas.

### Git tags

Document release convention in README or `CHANGELOG.md`:

```bash
git tag v0.1.0
git push origin v0.1.0
```

Tag format: `v` + SemVer from `Version.props`.

Optional but recommended:

- GitHub Release notes copied from `CHANGELOG.md`.

## Part 5 - UI Surfacing

Show the app version in the product UI.

Preferred location:

- **Setup modal** (`Open setup` gear button), because it already exists as the
  app-level control panel shell.

Suggested Setup content for this prompt:

- keep existing language selector behavior from prompt 007,
- add an **About** subsection containing:
  - app name: `ReVitae`,
  - app version: from `AppVersion.Current`,
  - optional informational version if different,
  - optional short text such as `Early preview` while `< 1.0.0`.

Requirements:

- version text must be localized where reasonable (labels, not the numeric version
  itself),
- do not add a separate native About window,
- do not add telemetry/update checks in this prompt.

Optional secondary location if easy:

- window subtitle/footer/debug area in development builds only.

Do not clutter the main CV editing header with permanent version text unless it
is subtle and consistent with the current design.

## Part 6 - Verification Scripts and Tests

Add guardrails so version drift is caught early.

### Script

Add something like:

```bash
./scripts/verify-version.sh
```

It should verify at minimum:

- `Version.props` app version matches README app badge,
- `package.json` version matches `Version.props` if synced by policy,
- optional: Windows manifest version matches expected assembly version format.

Wire the script into `./scripts/test.sh` or `npm run lint` only if it stays fast
and deterministic. Prefer `./scripts/test.sh` if it parses files without building.

### Tests

Add unit tests for:

- `AppVersion.Current` returns non-empty SemVer-like text,
- pre-release detection if implemented,
- README/version consistency if implemented as a test helper rather than shell
  script.

Keep tests deterministic in CI. Avoid requiring Git metadata unless commit hash
suffixes are explicitly part of the design and testable.

## Part 7 - Release Workflow (Manual v1)

Document the manual release steps for now:

1. Update `Version.props`.
2. Update `CHANGELOG.md`.
3. Update README app version badge.
4. Run:
   - `./scripts/format-cs.sh`
   - `npm run lint`
   - `./scripts/test.sh`
   - `./scripts/verify-version.sh`
5. Commit with a clear release message.
6. Tag `vX.Y.Z`.
7. Push commit and tag.
8. Create GitHub Release notes from `CHANGELOG.md`.

Future prompts may automate steps 1, 4, and 8 in CI. Do not implement CI release
automation here.

## Suggested File Changes

Expected new/updated files:

```text
Version.props
Directory.Build.props
CHANGELOG.md
scripts/verify-version.sh
src/ReVitae.Core/AppVersion.cs
src/ReVitae/MainWindow.axaml
src/ReVitae/MainWindow.axaml.cs
src/ReVitae/app.manifest
src/ReVitae.Core/Localization/TranslationKeys.cs
src/ReVitae.Core/Localization/AppLocalizer.cs
README.md
package.json
tests/ReVitae.Tests/AppVersionTests.cs
tests/ReVitae.Tests/VersionConsistencyTests.cs
prompts/020-app-versioning.md
```

Update README repository map prompt range to `001–020`.

## Acceptance Criteria

The prompt is complete when:

1. ReVitae has one canonical app version in `Version.props`.
2. Built app assemblies expose consistent version metadata.
3. Setup modal shows the current app version from runtime metadata.
4. README includes an app version badge separate from stack badges.
5. `CHANGELOG.md` exists with an `0.1.0` entry.
6. `package.json` no longer drifts from the app version policy.
7. Verification script and tests catch README/props drift.
8. `./scripts/format-cs.sh`, `npm run lint`, and `./scripts/test.sh` pass.

## Out of Scope

Do not implement in this prompt:

- auto-update checks,
- Sparkle/WinGet/homebrew release publishing,
- signed installers or notarization,
- CI tag-driven release automation,
- per-user licensing/version entitlements,
- CV document version history,
- database/schema versioning,
- prompt-number-based version bumps.

## Expected Result

After this prompt, a contributor or user can answer three distinct questions
without confusion:

1. **What version of ReVitae is this?**  
   Read the Setup/About section or README app badge (`0.1.0`).

2. **What stack does it use?**  
   Read README tech badges (`.NET 10`, `Avalonia 12`, etc.).

3. **How do we cut a new release?**  
   Follow `CHANGELOG.md`, bump `Version.props`, verify, tag `vX.Y.Z`, publish
   release notes.

Versioning becomes boring, explicit, and hard to accidentally break.
