# Prompt 034 - Static CV Quality Hints (Badge + Flyout)

Add **deterministic, non-blocking quality hints** that help users improve CV
content without changing data automatically. This implements Phase 1 guidance
from [`docs/concept.md`](../docs/concept.md): static rules, no AI.

Hints are **advisory only**. They must never block export, never overwrite user
text, and must remain visually distinct from **validation errors** (prompt **018**)
and **import confidence** styling.

Builds on prompts **002–016** (form sections), **018** (inline validation UI),
**017/021** (import drafts), and **023** (profile photo). Complements future
**save/load** prompt and Phase 2 AI — do not conflate with either.

## Goal

While editing a CV, the user sees **actionable suggestions** such as:

- missing sections that strengthen a typical CV,
- weak or generic work descriptions,
- summary length issues,
- duplicated contact URLs,
- skills grouping opportunities.

**Primary UX (decided):**

1. A **quality badge** on each relevant section header shows the hint count.
2. **Clicking the badge** opens a **flyout** anchored to the badge with the hint
   list for that section (desktop-first; not a auto-dismiss toast stack).
3. **Material snackbar** is used only for short global/transient messages (see
   **Material snackbar** section below), not as the main hint surface.

## Non-Goals

- AI / LLM recommendations (Phase 2),
- auto-editing or rewriting user content,
- blocking export when hints exist,
- replacing empty-state hints (`WorkExperienceEmptyHint`, etc.),
- persisting dismissed hints until save/load exists (optional follow-up),
- inline red error styling — hints are not validation failures.

## Architecture Overview

```text
Form UI (MainWindow + section views)
        │
        │  CvExportSourceData via CvExportSourceDataFactory
        │  (same snapshot export/validation already builds — do not read UI controls)
        ▼
┌───────────────────────────┐
│  CvQualityAnalyzer (Core)   │  ← NEW deterministic rules
└───────────────────────────┘
        │
        │  CvQualityReport { Hints[] }
        ▼
┌───────────────────────────┐
│  QualityHintsPresenter    │  ← NEW UI: badges + flyouts + snackbar edge cases
└───────────────────────────┘
```

**Keep analysis in Core, presentation in UI** — mirror `FieldValidator` +
`FormValidationService` separation.

### Do not merge with validation

| Concern           | Model                     | Blocks export? | Visual                             |
| ----------------- | ------------------------- | -------------- | ---------------------------------- |
| Validation        | `FieldValidationError`    | yes            | red inline + error badge           |
| Import confidence | `ImportedFieldConfidence` | no             | yellow import border               |
| **Quality hints** | **`CvQualityHint`**       | **no**         | **info/suggestion badge + flyout** |

Never add quality messages to `FieldValidationResult`.

## Core Implementation

### New types (`ReVitae.Core/Quality/`)

```csharp
public enum CvQualityHintSeverity
{
    Info,
    Suggestion
}

public sealed record CvQualityHint(
    string Id,
    string MessageKey,
    CvQualityHintSeverity Severity,
    CvImportSectionId? Section = null,
    string? FieldKey = null,
    string? EntryId = null);

public sealed record CvQualityReport(
    IReadOnlyList<CvQualityHint> Hints);
```

- **`Id`** — stable rule identifier (e.g. `work.generic-description`) for tests.
- **`MessageKey`** — localized via `TranslationKeys.QualityHint*`.
- **`Section`** — routes badge count per section.
- **`FieldKey` / `EntryId`** — optional scroll target for “Go to field” action.

### Snapshot input

**Reuse `CvExportSourceData`** (`ReVitae.Core/Export/CvExportSourceData.cs`) built
through existing **`CvExportSourceDataFactory.Create(...)`** — the same filtered
snapshot export uses (`HasUserInput()` entries only). Do **not** introduce a
parallel `CvDocumentSnapshot` type unless a field is truly missing (then extend
`CvExportSourceData` minimally).

Snapshot includes:

- `PersonalInformationImport` personal fields,
- active work / education / skills / languages / certificates / projects / links
  entries,
- additional information text,
- profile photo presence (via personal photo path or dedicated bool from UI when
  building the snapshot).

Add **`CvQualityGate.HasStartedCv(CvExportSourceData data)`** in Core:

- returns true when first or last name is non-empty **or** any repeatable section
  has at least one `HasUserInput()` entry,
- section-empty rules (`work.section-empty`, etc.) fire only when
  `HasStartedCv` is true — avoids nagging on a brand-new blank form.

Analyzers must **not** read Avalonia controls directly. `MainWindow` (or a small
UI helper) gathers field values and calls `CvExportSourceDataFactory` exactly
like export validation does today.

### Analyzer entry point

```csharp
public static class CvQualityAnalyzer
{
    public static CvQualityReport Analyze(CvExportSourceData snapshot);
}
```

Orchestrator runs section rule classes and concatenates results. De-duplicate by
`Id` + `EntryId` + `FieldKey` when the same rule fires twice.

### Rule modules (v1 minimum)

Implement as internal static classes under `ReVitae.Core/Quality/Rules/`:

| Rule ID                              | Trigger (summary)                                                                  | `CvImportSectionId`   |
| ------------------------------------ | ---------------------------------------------------------------------------------- | --------------------- |
| `personal.summary-too-short`         | summary non-empty and `< 80` non-ws chars                                          | `PersonalInformation` |
| `personal.summary-too-long`          | summary `> 600` non-ws chars                                                       | `PersonalInformation` |
| `personal.summary-missing`           | `HasStartedCv`, summary empty, work or education has entries                       | `PersonalInformation` |
| `personal.missing-title`             | `HasStartedCv` and professional title empty                                        | `PersonalInformation` |
| `work.section-empty`                 | `HasStartedCv`, zero active work entries, at least one **other** section has data  | `WorkExperience`      |
| `work.entry-missing-description`     | active entry, empty description                                                    | `WorkExperience`      |
| `work.generic-description`           | active entry; see heuristic below                                                  | `WorkExperience`      |
| `education.section-empty`            | `HasStartedCv`, zero active education entries, at least one other section has data | `Education`           |
| `skills.single-large-group`          | one group with `> 15` skills with `HasUserInput()`                                 | `Skills`              |
| `skills.section-empty`               | `HasStartedCv`, zero skill groups with any skills                                  | `Skills`              |
| `languages.section-empty`            | at least one active work entry, zero active languages                              | `Languages`           |
| `links.duplicate-personal-url`       | link URL matches personal LinkedIn/GitHub/portfolio (normalized)                   | `Links`               |
| `certificates.section-empty`         | `HasStartedCv`, zero active certificates, another section has data                 | `Certificates`        |
| `projects.section-empty`             | `HasStartedCv`, zero active projects, another section has data                     | `Projects`            |
| `projects.entry-missing-description` | active project, empty description and highlights                                   | `Projects`            |
| `import.review-section`              | section has `≥ 2` import fields with `Low` confidence                              | matching section      |
| `import.review-field`                | `Low` import confidence on field and a content rule still fires                    | matching section      |

**v1 has no rules yet** for Additional Information — still wire badge (count stays 0).

Add more rules only with unit tests.

**Generic description heuristic (v1):** for each active work entry with description
length `> 40` non-whitespace chars, flag when **all** of the following are true:

- no ASCII digit in description,
- no `%` character,
- no token from the small English verb allowlist (`increased`, `reduced`,
  `delivered`, `led`, `built`, …) matched case-insensitively as whole words.

Keep the allowlist in Core; localized message stays generic. Set **`FieldKey`** using
existing key builders (e.g. `WorkExperienceFieldKeys.Build(entryId,
WorkExperienceFieldKeys.Description)`) so flyout **Go to field** works with
`IValidationNavigableSection.ExpandAndRevealField(fieldKey)`.

### Empty vs quality hints

- **Empty hint** (existing, inside section body): section has zero entries —
  “Add your most recent role…”.
- **Quality hint** (new, on header badge): content can improve, or a section is
  missing relative to an otherwise started CV.

Do **not** fire `*.section-empty` on a totally blank new CV (`HasStartedCv ==
false`). When work is empty but the user has not started elsewhere, rely on the
existing empty-state copy — avoid duplicating the same message on the badge.

For **`links.duplicate-personal-url`**, normalize URLs the same way import does
(trim, lowercase scheme/host, strip trailing slash) before comparing link URL to
personal LinkedIn, GitHub, or portfolio fields.

Pass optional **`IReadOnlyList<ImportedFieldConfidence>`** into
`CvQualityAnalyzer.Analyze(...)` for import-aware rules; clear after **New CV**.

## UI Implementation

### Quality badge (section header)

Mirror **`ValidationErrorBadgeFactory`** pattern
(`src/ReVitae/Ui/Validation/ValidationErrorBadgeFactory.cs`) but with distinct
styling:

- new **`QualityHintBadgeFactory`** — icon `LightbulbOutline` or `InformationOutline`,
  **info/suggestion brush** (not `MaterialErrorBrush`),
- badge shows **count** of hints for that section,
- visible when count `> 0` (may show while expanded **and** collapsed — unlike error
  badge which only shows when collapsed; quality hints are informational).

Place badge in `ExpandableSection.HeaderActions` **next to** the existing error
badge. Use a horizontal **`StackPanel`** (or shared header-actions helper) so
both badges fit — `HeaderActions` currently holds only the error badge panel in
each `*SectionView`.

Sections with quality badge wiring in v1:

- Work, Education, Skills, Languages, Certificates, Projects, Links,
  Additional Information,
- personal information block in `MainWindow` (same header row as personal error badge).

Add **`QualityHintsService`** in `ReVitae.Ui/Quality/` mirroring
`FormValidationService.UpdateSectionErrorBadge` — keeps section views thin.

Wire through each `*SectionView` via a small interface, e.g.:

```csharp
public interface IQualityHintSection
{
    void ApplyQualityHints(IReadOnlyList<CvQualityHint> sectionHints);
}
```

**Navigation:** reuse **`IValidationNavigableSection.ExpandAndRevealField(fieldKey)`**
for flyout **Go to field** — populate `FieldKey` with the same composite keys
validation uses (`WorkExperienceFieldKeys.Build`, etc.). Optional `EntryId` on
`CvQualityHint` is for tests/logging only; navigation must go through `FieldKey`.

### Flyout on badge click (primary UX)

On badge **`PointerPressed`** and **`KeyDown`** (Enter/Space when focused):

1. Open **`Flyout`** / `Popup` anchored to the badge (Avalonia built-in — prefer
   over third-party).
2. Content:
   - localized section title (`QualityHintFlyoutTitle` with `{0}` = section name),
   - bulleted list of hint messages (`AppLocalizer.Get(hint.MessageKey)`),
   - optional **“Go to field”** button per hint when `FieldKey` is set —
     call `IValidationNavigableSection.ExpandAndRevealField(hint.FieldKey)`.
3. Flyout stays open until user clicks away or presses Escape — **no auto-dismiss**
   for multi-hint lists.
4. If exactly **one** short hint and user prefers minimal UI, flyout may be a
   compact single-card — still not a toast stack.

Set **`AutomationProperties.Name`** on the badge (count + “suggestions”) for screen
readers. Use **`QualityHintSeverity`** to pick icon/color per row in the flyout
(Info vs Suggestion).

Do **not** show one toast per hint sequentially.

### Material snackbar (secondary / transient only)

Add **`SnackbarHost`** to `MainWindow` root (Material.Avalonia — already a
dependency; verify exact API name in the installed Material.Avalonia version
during implementation) for:

- optional one-liner after import: “Import finished — review suggestions in section
  badges” (`QualityHintSnackbarAfterImport`),
- optional global nudge when **total** hint count crosses threshold first time in
  session (e.g. `≥ 3` hints and user never opened a flyout) — show **once** per app
  session, not on every keystroke.

Do **not** use snackbar as the main hint list UI.

### Refresh timing

Add **`UpdateQualityHints()`** in `MainWindow`, called from the same places as
`UpdateValidationState()`:

- field changes,
- entry add/remove/reorder,
- import apply,
- profile photo change,
- language switch (rebind localized flyout labels only).

Avoid heavy re-analysis on every keypress if costly — acceptable v1: run with
validation batch; optimize later with debounce if needed.

### Coexistence with validation badge

When both error count and hint count are non-zero:

- show **both** badges (error left or primary, quality second),
- error badge click keeps existing behavior: **expand section only** (see
  `FormValidationService.UpdateSectionErrorBadge` + `expandSection` callback —
  scroll-to-first-error happens on **export failure**, not on badge click),
- quality badge opens flyout only.

## Localization

Add keys to `TranslationKeys.cs` and `AppLocalizer` (English base + existing
overlay pattern for SK/CS as time permits). Register every new key in the
**RequiredKeys** list (same discipline as prompts **018** / **021**) so missing
translations fail tests early:

| Key                                    | English example                                                                 |
| -------------------------------------- | ------------------------------------------------------------------------------- |
| `QualityHintFlyoutTitle`               | Suggestions — {0}                                                               |
| `QualityHintGoToField`                 | Go to field                                                                     |
| `QualityHintBadgeCount`                | {0}                                                                             |
| `QualityHintSnackbarAfterImport`       | Import complete. Review suggestions on section badges.                          |
| `QualityHintSnackbarFirstSession`      | This CV has suggestions to improve — click the badges beside sections.          |
| `QualityHintPersonalSummaryTooShort`   | Short summary — consider adding 2–3 sentences about your role and strengths.    |
| `QualityHintPersonalSummaryTooLong`    | Summary is very long — recruiters often skim; consider shortening.              |
| `QualityHintPersonalMissingTitle`      | Professional title is empty — add a headline under your name.                   |
| `QualityHintWorkSectionEmpty`          | No work experience yet — add at least your most recent role.                    |
| `QualityHintWorkMissingDescription`    | A work entry has no description — add responsibilities or achievements.         |
| `QualityHintWorkGenericDescription`    | Description looks generic — add measurable outcomes (numbers, scope, impact).   |
| `QualityHintEducationSectionEmpty`     | No education entries — add your highest qualification if relevant.              |
| `QualityHintSkillsSingleLargeGroup`    | Many skills in one group — split into categories (e.g. Frontend, Backend).      |
| `QualityHintSkillsSectionEmpty`        | No skills listed — add core technologies or tools you use.                      |
| `QualityHintLanguagesSectionEmpty`     | No languages listed — add languages you can use professionally.                 |
| `QualityHintLinksDuplicatePersonalUrl` | A link duplicates a URL already in personal information — consider removing it. |

Section names in flyout title should reuse existing section title keys where
possible.

## Testing

### Core unit tests (`tests/ReVitae.Tests/Quality/`)

- `CvQualityAnalyzerTests` — one test per rule ID, positive and negative cases,
- snapshot builders with minimal entries (no UI),
- de-duplication when multiple rules could overlap,
- `*.section-empty` rules do not fire when `HasStartedCv` is false,
- post-import: run analyzer on applied snapshot and assert expected hint counts
  for a fixture CV (Core-only, no UI).

### UI tests (optional v1)

Manual QA checklist is sufficient for v1; automated UI tests not required.

## Documentation Updates

- [`docs/concept.md`](../docs/concept.md) — mark Phase 1 static hints as implemented,
- [`README.md`](../README.md) — one bullet under Current Highlights; roadmap item
  “static CV quality hints” → done,
- cross-link this prompt from concept Phase 1 section.

## Out of Scope (This Prompt)

- AI-generated suggestions,
- hint dismiss / snooze persistence across app restarts (session-only dismiss is in scope),
- per-field inline hint text under every TextBox (flyout is enough for v1),
- quality scoring / letter grade / percentage,
- template-specific hints (“this template expects a photo”),
- snackbar queue for every hint.

## Follow-Up Additions (included in v1)

### 1. `personal.summary-missing`

When `HasStartedCv`, summary is empty, and at least one of work or education has
active entries — suggest adding a summary (distinct from too-short/too-long).

### 2. Certificates and Projects rules

| Rule ID                              | Trigger                                                            | Section        |
| ------------------------------------ | ------------------------------------------------------------------ | -------------- |
| `certificates.section-empty`         | `HasStartedCv`, zero active certificates, another section has data | `Certificates` |
| `projects.section-empty`             | `HasStartedCv`, zero active projects, another section has data     | `Projects`     |
| `projects.entry-missing-description` | active project with empty description and highlights               | `Projects`     |

### 3. Session-only dismissal (`QualityHintDismissalStore`)

UI store (not persisted) lets user **Dismiss** a hint from the flyout for the
current session. Filter hints before badge counts. Persistence waits for save/load.

### 4. Export-area quality summary

Show a non-blocking line above **Export** when total hint count `> 0`
(`QualityHintExportSummary`, e.g. “{0} suggestions — see section badges”).
Optional **Review** button jumps to the first section with hints.

### 5. Import-aware hints

Pass optional `IReadOnlyList<ImportedFieldConfidence>` into analysis (stored after
import in `MainWindow`):

| Rule ID                 | Trigger                                                                                                            |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `import.review-section` | section has `≥ 2` fields tagged `CvImportConfidence.Low`                                                           |
| `import.review-field`   | field has `Low` confidence **and** a content rule still fires for that field (e.g. empty/generic work description) |

Distinct message keys; do not reuse import yellow border styling.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` and `./scripts/lint-cs.sh` pass,
- `npm run lint` passes if docs touched,
- full test suite passes,
- manual: fill work entry with “Responsible for various tasks” → badge on Work →
  flyout shows generic-description hint,
- manual: fix description with “Increased revenue 20%” → hint clears on next refresh,
- manual: validation error + quality hint both visible, export still blocked only
  by validation,
- manual: import CV → optional snackbar once; badges populated,
- error badge behavior unchanged (prompt 018 regression).

## Summary for Implementers

**Quality hints = Core rules + section badges + flyout.** Use Material snackbar
only for short global messages. Never mix with validation errors. Never block
export. Click badge → read suggestions → optional jump to field. Prompt **039**
adds an optional **Improve with AI** button on supported hints — deterministic
rules remain the source of truth; AI only suggests text the user must accept.
