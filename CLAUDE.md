# Claude rules for ReVitae

Project-specific working rules. These are binding — follow them in every session.

## Commits & git

- **Never add Claude/Anthropic as a commit co-author.** Do not append a
  `Co-Authored-By: Claude ...` trailer (or any AI co-author) to commit messages or
  PR bodies. Commits are authored by the repository owner only.
- Keep commit messages factual and scoped to the change; no AI attribution lines.
- `.claude/` is git-ignored and must never be committed.
- `prompts/` is git-ignored (local planning docs) — never commit prompt files.
- Only commit or push when explicitly asked. Branch off `main` for feature work.

## Versioning a release

Three version concepts must stay in sync when bumping the app version
(`Version.props` `VersionPrefix` / `AssemblyVersion`):

- `package.json` `"version"`,
- `src/ReVitae/app.manifest` `assemblyIdentity version` (`X.Y.Z.0`),
- README app badge (`badge/app-X.Y.Z-blue`),
- `CHANGELOG.md` section,
- the hardcoded baseline in `tests/ReVitae.Tests/AppVersionTests.cs`.

Verify with `./scripts/verify-version.sh`. Version-consistency tests will fail
otherwise.

## Tests

- After adding tests, update `TestCountBaselineTests.MinimumTestCount` **and** the
  README test badge to the new total; they must match. Verify with
  `./scripts/verify-test-count.sh`.
- Business logic goes in `ReVitae.Core` (testable); Avalonia UI stays thin wiring.
  Extend Core before adding UI tests — UI section views are not headless-tested.

## Before committing

- Run `npm run lint` (markdownlint + `dotnet format --verify-no-changes` + Release
  build + full test). The pre-commit hook runs it; do not bypass it.
- Markdown must pass `markdownlint-cli2`; format markdown with Prettier if needed.
