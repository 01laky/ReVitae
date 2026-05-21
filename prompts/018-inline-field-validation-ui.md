# Prompt 018 - Inline Field Validation UI Refactor

Refactor validation feedback across the entire ReVitae form so errors behave like
modern web forms: **red inline messages directly under the related input**, with
**invalid controls visually highlighted**. Remove the current bottom-of-form error
dump.

## Goal

Fix the validation UX so users can immediately see **which field is wrong** and
**why**, without scrolling to a long duplicated error list at the bottom of the
form.

This is a **UI refactor only**. Keep the existing Core validation rules,
`FieldValidator`, collection validators, translation keys, and `FieldValidationError`
model. Do not change business validation semantics unless a small adjustment is
required to map an existing error to the correct visible control.

## Problem Statement (Current Inspect Findings)

### What works today

Core validation is solid:

- `FieldValidationResult` / `FieldValidationError` in `ReVitae.Core/Validation`
- section validators for personal info and all repeatable sections
- live validation on text change via `UpdateValidationState()` in `MainWindow`
- many sections already create per-field `TextBlock` elements with class
  `re-vitae-error` under inputs through duplicated `CreateField(...)` helpers

Personal information in `MainWindow.axaml` already has dedicated error text blocks
under each field (`FirstNameErrorTextBlock`, `EmailErrorTextBlock`, etc.), updated
through `UpdateFieldErrorMessages(...)` in `MainWindow.axaml.cs`.

Repeatable section entry cards in:

- `WorkExperienceSectionView`
- `EducationSectionView`
- `SkillsSectionView`
- `LanguagesSectionView`
- `CertificatesSectionView`
- `ProjectsSectionView`
- `LinksSectionView`
- `AdditionalInformationSectionView`

already route validator errors to per-field `_errorTextBlocks` dictionaries.

### What is broken / unacceptable

1. **`ValidationSummaryTextBlock` duplicates every error at the bottom of the
   form**

   In `MainWindow.axaml` and `MainWindow.axaml.cs`:

   ```csharp
   ValidationSummaryTextBlock.Text = validationResult.IsValid
       ? string.Empty
       : string.Join(Environment.NewLine, validationResult.Errors.Select(...));
   ```

   This produces a long repeated list such as:
   - `Start year is required.`
   - `End month is required.`
   - `End year is required.`

   repeated for every invalid project/work/education entry. This is the primary UX
   failure shown in current screenshots.

2. **Errors are duplicated for some fields and completely missing inline for
   others**

   Scalar field errors in expanded entry cards often appear both inline and again
   in `ValidationSummaryTextBlock`.

   Some nested errors never reach an inline control today and exist **only** in
   the removed summary, for example:
   - `skills.{groupId}.{skillId}.proficiency` and
     `skills.{groupId}.{skillId}.yearsOfExperience` in `SkillsSectionView` —
     `_errorTextBlocks` registers `category`, `name`, and `skills`, but not
     per-chip proficiency/years targets,
   - project technology errors are all routed to the generic add-technology row,
     not to the specific chip that failed.

   Removing the summary without fixing these mappings would silently hide errors.

3. **Personal info inline errors are incomplete**

   Personal fields already render localized messages under the input, but they do
   not toggle `IsVisible`, do not highlight the input border, and still compete
   visually with the bottom summary dump.

4. **Invalid inputs are not visually highlighted**

   Only the error text uses `MaterialErrorBrush`. The related `TextBox`,
   `ComboBox`, or `AutoCompleteBox` keeps the default border, unlike common web
   form behavior.

5. **Empty error text blocks still participate in layout inconsistently**

   Error text blocks are always present but often left with empty `Text`. The
   prompt should standardize visibility (`IsVisible`) and invalid styling on the
   actual input control.

6. **Collapsed entry cards hide inline field errors**

   Entry cards show a header badge (`"{0} errors"`) only when collapsed. That is
   acceptable as a secondary indicator, but the user still needs a reliable way
   to reach the exact invalid field.

7. **Some nested/collection errors do not map cleanly to a visible control**

   Examples:
   - duplicate project technology errors keyed to a specific technology ID, but UI
     maps broadly to the add-technology input,
   - skills collection-level errors mapped to bulk skills input,
   - date-range cross-field errors merged into the issue/start-month error block in
     a non-obvious way (Work Experience, Education, Projects, Certificates).

8. **No scroll-to-first-error behavior**

   `OnExportPdfClicked` already calls `UpdateValidationState(...)` and sets
   `ExportStatusTextBlock` via existing `TranslationKeys.ExportFixValidation`,
   but the form does not expand collapsed sections/cards or scroll the left
   `ScrollViewer` to the first invalid control.

9. **Massive duplicated UI code**

   Nearly every section view reimplements the same `CreateField`, `CreateDateField`,
   and `UpdateValidation` patterns by hand. This refactor should consolidate that
   behavior.

## Product Behavior Summary

After this prompt:

1. **No global validation dump** at the bottom of the form.
2. Every validation error appears **once**, directly under the related control.
3. Invalid controls get a clear red invalid state.
4. Collapsed sections/cards may still show compact error indicators, but must not
   be the only way to discover errors.
5. Attempting export with invalid data should reveal the first problem in context.
6. Validation remains live while editing; export remains blocked until valid.

## Non-Goals

Do **not** implement in this prompt:

- save/load project persistence,
- changes to PDF export rendering,
- new validation rules or new required fields,
- AI recommendations,
- replacing Core validators with a different validation library,
- toast/snackbar-only error UX,
- modal-based validation summaries.

Do **not** reintroduce a full-form error list somewhere else. If a compact
non-field-specific message is needed, it must be a short action hint near the
Export button, not a repeated dump of field messages.

## Part 1 - Shared Validation UI Infrastructure

Introduce reusable UI helpers/controls under `src/ReVitae/Ui/` (exact names can
vary, but behavior must match).

Suggested components:

### `ValidatedFormField`

A small composable wrapper for:

- label,
- input control,
- optional helper/counter below input,
- inline error text directly below the input.

Requirements:

- stores mapping from logical field name / field key suffix to:
  - input control,
  - error `TextBlock`,
- exposes methods such as:
  - `SetErrorMessages(IReadOnlyList<string> messages)`
  - `ClearErrors()`
  - `HasErrors`
- toggles:
  - error text visibility,
  - invalid styling class on the input control.

Suggested layout order inside one field:

```text
Label
Input
Helper / counter (optional)
Error message(s) in red
```

This matches standard web form structure.

For multiline fields, keep the character counter **above** the error text when a
counter exists. The error must remain the last feedback element directly under the
validated control area.

Current code is inconsistent (`CreateMultilineField` appends counters after the
error in some section views; `AdditionalInformationSectionView` places the counter
before the error). Standardize on: `label → input → counter → error`.

### `ValidatedFieldBinding` or static helper

Create a shared helper to reduce duplication in section views, for example:

- `ValidatedFieldRegistry`
- `FormValidationPresenter`

Responsibilities:

- register fields once during card construction,
- apply a filtered list of `FieldValidationError` objects to the correct controls,
- localize messages through `AppLocalizer`,
- deduplicate identical messages for the same field,
- set invalid classes on all related inputs.

The helper must support:

- simple scalar fields (`firstName`, `email`, `name`, `url`),
- paired date fields through a shared `ValidatedDateRangeField` component (see
  Part 11),
- cross-field date-range errors,
- nested collection item fields keyed by entry/skill/technology IDs.

Do not keep eight separate handwritten versions of the same logic if the shared
helper can cover them.

### `ValidatedDateRangeField`

Extract the repeated month/year row pattern from Work Experience, Education,
Projects, and Certificates into one shared date-range field wrapper.

Requirements:

- label,
- month `ComboBox` + year `TextBox` row,
- inline required errors for month/year,
- dedicated inline area for cross-field `DateRange` errors (`start after end`,
  `issue after expiration`),
- invalid styling applied to the specific month/year control that failed,
- do not merge date-range messages into unrelated scalar fields.

All four date-bearing sections must use this shared component instead of local
`CreateDateField(...)` copies.

## Part 2 - Visual Styling

Extend `src/ReVitae/Themes/ReVitaeMaterialStyles.axaml` and `UiClasses.cs`.

Add a new invalid-state class, for example:

- `re-vitae-field-invalid`
- `re-vitae-chip-invalid` for skill/project technology chips in `UiClasses.cs`

Apply `re-vitae-field-invalid` to invalid controls:

- `TextBox`
- `ComboBox` (including month selectors in date rows)
- `AutoCompleteBox`

Do not apply invalid styling to non-input controls such as `CheckBox` toggles
(`Currently working`, `Currently active`).

For chip invalid state:

- apply `re-vitae-chip-invalid` to the specific `Border.re-vitae-skill-chip`
  (or equivalent project technology chip) that owns the error,
- use `MaterialErrorBrush` border/background emphasis consistent with theme,
- expose the localized error through chip `ToolTip` text in addition to inline
  message near the chip row,
- remove chip invalid styling when the underlying item becomes valid.

Requirements:

- use `MaterialErrorBrush` or equivalent theme error color for border,
- slightly stronger border thickness than normal,
- works in light and dark theme,
- remove invalid class immediately when the field becomes valid again.

Keep error text styling on `TextBlock.re-vitae-error`, but ensure:

- font size is readable under the field,
- `TextWrapping = Wrap`,
- `IsVisible = False` when there is no message,
- no extra vertical gap when hidden.

Optional but recommended:

- invalid label text may remain normal; do **not** turn the whole form red,
- only the input border + inline message should signal error.

## Part 3 - MainWindow Personal Information

Refactor `MainWindow.axaml` / `MainWindow.axaml.cs` to use the shared validation
field pattern.

Requirements:

- keep existing field layout order,
- keep live validation on `TextChanged`,
- replace manual `UpdateFieldErrorMessages(...)` field-by-field assignment with the
  shared presenter/registry,
- remove `ValidationSummaryTextBlock` from XAML and all code references.

If useful, migrate personal-info fields from hand-written XAML error blocks to a
consistent structure, but do not regress accessibility names/tooltips.

Personal info fields covered:

- first name,
- last name,
- professional title,
- email,
- phone,
- location,
- LinkedIn URL,
- portfolio / website URL,
- GitHub URL,
- short summary.

## Part 4 - Repeatable Section Entry Cards

Apply the shared inline validation pattern consistently in all repeatable section
views:

- Work Experience
- Education
- Skills
- Languages
- Certificates
- Projects
- Links
- Additional Information

For each section:

1. Replace duplicated `CreateField` / `CreateDateField` error wiring with the
   shared validation field helper.
2. Ensure `SectionView.UpdateValidation(FieldValidationResult)` still filters
   errors by entry ID / group ID using existing `*FieldKeys.TryParse...` helpers.
3. Keep collapsed-entry header badge behavior, but improve discoverability:
   - when a collapsed entry has errors, clicking the badge or error indicator
     should expand that entry card,
   - optionally auto-expand the parent section if it is collapsed and contains
     invalid entries.

### Date fields

Use the shared `ValidatedDateRangeField` component (Part 1 / Part 11) in all
date-bearing sections.

Requirements:

- month-required → under month control or first line of the date error area,
- year-required → under year control or second line of the date error area,
- date-range invalid (`start after end`, `issue after expiration`) → dedicated
  red message in the date-range error area,
- do not merge date-range messages into unrelated scalar fields.

### Collection/nested item errors

Handle these explicitly:

#### Skills

- group category errors → under category field,
- skill name/proficiency/years errors → under the corresponding skill input area,
- `SkillsCollection` empty-collection error → under bulk skills input with one
  clear message.

This section requires a real fix, not just cleanup:

- add presenter targets for per-skill `proficiency` and `yearsOfExperience`
  field keys,
- when a skill exists only as a chip, show the error on/near that chip or in a
  dedicated inline area directly below the chip row,
- apply `re-vitae-chip-invalid` to the affected chip and set chip tooltip text to
  the localized error,
- do not route chip-only proficiency/years errors to the generic `SkillName`
  add-row field unless that row is the actual source of the invalid value.

If a specific skill chip has an error, prefer highlighting that chip and/or
showing a small red message near the skill chip row when feasible. At minimum,
do not lose the error entirely when the summary block is removed.

#### Projects technologies

When a technology-specific validator error exists:

- show it near the technology editor/chip area,
- if the invalid technology is represented as a chip, visually mark that chip with
  `re-vitae-chip-invalid` and tooltip text,
- duplicate-technology errors must not disappear because the UI only knows about
  the generic add-technology field key.

#### Links / Languages / Certificates / Education / Work Experience

Use the same one-error-one-place rule. No section may rely on the removed global
summary.

## Part 5 - Section-Level Expandable Headers

Top-level `ExpandableSection` headers currently do not show validation state.
Entry cards already use `ExpandableSection.HeaderActions` for drag handles and
collapsed error badges; top-level section views do not.

Add a compact section-level invalid indicator when:

- the section is collapsed, and
- any entry/content inside it has validation errors.

Requirements:

- implement through `ExpandableSection.HeaderActions` where possible, reusing
  the same badge/icon pattern as entry cards,
- localized short text — prefer reusing existing `{0} errors` keys
  (`WorkExperienceValidationErrors`, `ProjectsValidationErrors`, etc.) for
  consistency; add a new key only where no section-specific key exists,
- red styling consistent with entry-card badges,
- clicking the indicator expands the section,
- do not duplicate every field message in the section header; count/summary only.

This applies to:

- `PersonalInformationSection` in `MainWindow.axaml`,
- every repeatable section view's top-level `_section`,
- `AdditionalInformationSectionView`.

`ExpandableSection.axaml` already exposes `HeaderActionsPresenter`; modify the
control only if the shared header badge cannot be hosted through existing API.

## Part 6 - Export Failure UX

When the user clicks **Export PDF** and validation fails:

1. run validation (existing behavior),
2. update all inline field errors through `UpdateValidationState(...)` (existing
   behavior),
3. **do not** populate a global summary list,
4. reveal the first invalid field:
   - expand the containing section if collapsed,
   - expand the containing entry card if collapsed,
   - scroll the left form `ScrollViewer` so the first invalid control is visible,
5. keep using existing `ExportStatusTextBlock` + `TranslationKeys.ExportFixValidation`
   near the Export button; do not add a second generic export error message.

Name the left form scroll container in XAML, for example `FormScrollViewer`, so
scroll targeting is reliable from code-behind.

Requirements:

- first-invalid-field order should be deterministic and match visual form order:
  personal info → work experience entries → education → skills → languages →
  certificates → projects → links → additional information,
- within a repeatable section, use current visual order of entry cards,
- do not steal focus in a way that breaks IME/text editing unless necessary; scroll
  into view is required, focus is optional but recommended for the first invalid
  text input.

Add a small shared helper in UI layer, for example
`ValidationNavigation.ScrollToFirstInvalid(...)`.

Section views should expose enough metadata for navigation, for example:

- `ScrollToFirstInvalid()` on each section view, or
- a shared `IValidationNavigableSection` contract implemented by personal info
  wrapper logic in `MainWindow` and all section views.

## Part 7 - Validation Lifecycle Rules

Keep existing validation **computation** timing:

- validate on field changes,
- validate on section entry add/remove/reorder,
- validate on import apply,
- validate before export.

Add a shared `ValidationInteractionMode` / field touch tracking layer in the UI
(see Part 11) to control **when errors become visible**.

Clarify display rules:

- when a field has no errors:
  - clear error text,
  - remove invalid class from input,
- when a draft/inactive entry has no user input:
  - no errors shown (existing `HasUserInput()` behavior stays),
- when a field has multiple errors:
  - show all unique messages under that field, each on its own line,
- localized messages must continue to come from translation keys via
  `_localizer.Get(error.Message)`.

Do not validate hidden/disabled end-date fields when `Currently working` /
`Currently active` is checked. Existing Core logic already skips those; preserve
it.

## Part 8 - Localization

Add any new UI strings to:

- `TranslationKeys.cs`
- `AppLocalizer.cs`
- `TranslationKeys.RequiredKeys`
- `tests/ReVitae.Tests/TranslationKeysTests.cs`
- `tests/ReVitae.Tests/LocalizationTests.cs`

Reuse existing keys where possible:

- `TranslationKeys.ExportFixValidation` for export failure status text,
- existing `{0} errors` section/card keys for compact badges.

Add new keys only if a section needs a summary string that cannot reuse an
existing pattern.

Do not hardcode English in new UI code.

## Part 9 - Tests

Add **comprehensive edge case tests** for all new validation UI behavior. Follow
the repository convention used by import tests (`CvImportEdgeCaseTests`, etc.):
group related cases into focused test classes with explicit edge-case names.

Do not rely on full Avalonia UI automation unless the repo already has that
infrastructure. Test presenter/helper/navigation/touch-tracking logic directly.

Keep all existing Core validation tests passing unchanged unless a test was
asserting the old summary behavior.

### Required test files

Add tests under `tests/ReVitae.Tests/Ui/Validation/`, for example:

- `ValidationPresenterEdgeCaseTests.cs`
- `ValidationTouchTrackingEdgeCaseTests.cs`
- `ValidatedDateRangeFieldEdgeCaseTests.cs`
- `ValidationNavigationEdgeCaseTests.cs`
- `ValidationAccessibilityEdgeCaseTests.cs`
- `ValidationOrphanErrorsEdgeCaseTests.cs`

Exact file split may vary, but every area below must be covered and none may be
left with only a single happy-path test.

### 9.1 - Presenter / field mapping edge cases

Cover at minimum:

- scalar personal-info keys (`firstName`, `email`, `shortSummary`, URL fields),
- entry-prefixed keys for every repeatable section
  (work, education, skills, languages, certificates, projects, links),
- unknown / malformed field keys → safely ignored without crash,
- duplicate `FieldValidationError` entries for the same field → one deduplicated
  inline message,
- multiple different errors on the same field → all unique messages, stable order,
- empty `FieldValidationResult` → all registered targets cleared,
- switching from invalid to valid → error text cleared, invalid class removed,
- localization: messages resolved through translation keys, not raw key strings,
- unsupported language overlay still resolves English fallback message text.

Section-specific mapping edge cases:

- **Work Experience / Education / Projects**: `startMonth`, `startYear`, `endMonth`,
  `endYear`, and `dateRange` mapped to `ValidatedDateRangeField` targets,
- **Certificates**: `issueMonth`, `issueYear`, `expirationMonth`,
  `expirationYear`, and issue/expiration `dateRange`,
- **Skills**: group `category`, bulk `skills`, add-row `name`, per-chip
  `proficiency`, per-chip `yearsOfExperience`, duplicate-in-group errors,
- **Projects**: generic add-row technology errors and per-chip technology ID
  errors,
- **Links**: duplicate URL keyed to the duplicate entry, not the first entry,
- **Languages**: duplicate language, invalid CEFR/proficiency combinations,
- **Additional Information**: max-length/content errors on the single content field.

### 9.2 - Touch-aware display edge cases

Cover at minimum:

- untouched invalid field → no inline error, no invalid class,
- field touched on blur → inline error appears if still invalid,
- field edited after failed export → becomes touched and shows inline error,
- failed export with invalid form → all currently invalid fields become touched,
- valid field after touch → error hidden immediately,
- draft entry with no `HasUserInput()` → no errors shown and no touch state needed,
- import-populated low-confidence field → treated as touched for display purposes,
- toggling `Currently working` / `Currently active`:
  - end-date errors disappear,
  - end-date touch/invalid state cleared,
  - start-date errors unaffected,
- re-import / replace entries resets touch state for replaced controls where
  appropriate.

### 9.3 - `ValidatedDateRangeField` edge cases

Cover at minimum:

- missing month only,
- missing year only,
- missing both month and year on start/end,
- invalid month value,
- invalid year value,
- valid start/end with `dateRange` error (`start after end`,
  `issue after expiration`),
- currently working/active hides end-date required errors and invalid styling,
- month invalid + year invalid + dateRange invalid simultaneously → all visible
  in distinct areas without overwriting each other,
- clearing one part of the date removes only the corresponding error state.

Apply the same edge-case matrix to all four date-bearing sections through shared
helper tests; do not copy-paste four identical test suites unless parameterized.

### 9.4 - Chip invalid state edge cases

Cover at minimum:

- invalid skill chip by proficiency,
- invalid skill chip by years of experience,
- invalid project technology chip by duplicate name,
- invalid project technology chip by empty/invalid name,
- multiple invalid chips in one entry → each chip styled independently,
- fixing one chip clears only that chip's invalid state,
- chip invalid + row-level inline message both present,
- chip tooltip/help text uses localized message.

### 9.5 - Navigation / first-invalid-field edge cases

Cover at minimum:

- first error in personal info when all later sections valid,
- first error in later section when personal info valid,
- first error inside second entry card of a repeatable section,
- first error inside collapsed section/card → navigation target resolves to that
  field key even before UI expand actions run,
- deterministic ordering across full form order,
- no invalid fields → navigation helper returns null/no-op safely,
- multiple errors same section → first in visual card order wins.

### 9.6 - Accessibility edge cases

Cover at minimum:

- invalid field sets non-empty help text,
- valid field clears help text,
- multiple messages combined into one help text string,
- empty/no-error state does not leave stale help text from previous validation,
- chip invalid state exposes tooltip/help text on the chip or owning control.

### 9.7 - No orphan validator errors regression

Expand Part 11.5 into a full edge-case suite, not one fixture.

Create representative invalid fixtures covering **every section**, including:

- personal information,
- work experience,
- education,
- skills (chip-only skill data),
- languages,
- certificates,
- projects (technology chips),
- links,
- additional information.

For each fixture and for combined multi-section fixtures:

1. run the existing Core validation path used by `ValidateForm()`,
2. collect all `FieldValidationError.FieldKey` values,
3. pass them through the shared UI validation presenter/registry mapping,
4. assert that **every** error key resolves to a registered UI target,
5. assert that no mapped target is left without a message when an error exists.

Suggested test class/name direction:

- `ValidationOrphanErrorsEdgeCaseTests`
- `NoOrphanValidatorErrors_ForRepresentativeInvalidFixtures`
- `NoOrphanValidatorErrors_ForCombinedMultiSectionFixture`

If a validator error cannot be mapped to UI, the test must fail and the
presenter/section wiring must be fixed. Do not reintroduce a summary dump as a
fallback bucket for unmapped errors.

### 9.8 - Regression tests for removed summary behavior

Add explicit tests/assertions that:

- no code path builds a full-form joined validation message list,
- `ValidationSummaryTextBlock` is not referenced,
- export failure uses only `ExportFixValidation` as the generic message.

### Test quality bar

Match the depth of existing project test suites:

- use `[Theory]` + `[InlineData]` for boundary matrices where appropriate,
- prefer parameterized tests over duplicated methods,
- name tests after the edge case being protected,
- every new public helper/class added for this prompt must have direct unit tests,
- do not merge unrelated assertions into one vague test.

Before marking the prompt complete, run:

```bash
./scripts/test.sh
npm run lint
```

All existing tests plus all new edge case tests must pass.

## Part 11 - Additional Required Enhancements

These five enhancements are **in scope** for this prompt, not follow-up work.

### 11.1 - Validation interaction mode (touch-aware display)

Introduce UI-level touch tracking so validation feedback behaves more like modern
web forms.

Requirements:

- keep computing validation live on every change (existing Core behavior),
- track whether a field/control has been **touched**,
- a field becomes touched when any of the following happens:
  - user leaves the control (`LostFocus` / blur),
  - user edits the control after a failed export attempt,
  - export validation reveals that field as invalid,
  - import apply populates the field with low-confidence/review-worthy data,
- before a field is touched, do **not** show inline error text or invalid styling
  for that field, even if validator results already contain errors,
- after touch, show inline errors normally until resolved,
- failed export sets a session flag such as `HasAttemptedExportWithInvalidForm`
  that marks all currently invalid fields as touched so errors become visible in
  context,
- clearing/fixing a field removes its invalid presentation immediately once valid.

This reduces noisy red forms right after PDF import while preserving strict
feedback once the user starts editing or tries to export.

Implement touch tracking in shared validation UI code, not separately in every
section view.

Edge-case automated coverage for this behavior is required in Part 9.2.

### 11.2 - Accessibility for invalid fields

Invalid fields must be usable with assistive technologies, not only visually red.

For every invalid control, set accessibility metadata when errors are visible:

- `AutomationProperties.SetHelpText(control, localizedErrorText)` for the primary
  error message,
- if Avalonia version/project conventions support it, set invalid/accessibility
  state on the control in a way screen readers can consume,
- ensure error text blocks are associated with the input through help text rather
  than relying on visual proximity alone,
- when multiple errors exist on one field, help text should contain the combined
  unique messages,
- remove/help-text reset when the field becomes valid.

Add manual QA for keyboard navigation:

- tab to invalid field → screen reader/help text exposes the error,
- fixing the value clears help text without leaving stale accessibility state.

Automated edge-case coverage for help text behavior is required in Part 9.6; manual
QA is supplementary, not a substitute.

### 11.3 - Shared `ValidatedDateRangeField`

This enhancement is required and is also referenced in Part 1 and Part 4.

Create one reusable date-range field component used by:

- Work Experience start/end dates,
- Education start/end dates,
- Projects start/end dates,
- Certificates issue/expiration dates.

It must replace local `CreateDateField(...)` implementations and eliminate ad-hoc
merging of `DateRange` errors into start/issue month blocks.

Acceptance for this component:

- one consistent visual/interaction pattern across all four sections,
- month/year required errors and cross-field range errors are visually distinct,
- invalid month `ComboBox` and invalid year `TextBox` styling works independently.

Edge-case automated coverage is required in Part 9.3.

### 11.4 - Chip-level invalid state

For nested collection items rendered as chips, inline error text alone is not
enough.

Requirements:

- add `re-vitae-chip-invalid` style in theme,
- apply it to the exact skill or project technology chip that owns the validator
  error,
- set chip tooltip to the localized error message,
- keep a concise inline error near the chip row when a chip is invalid,
- remove chip invalid styling when that item validates cleanly.

This applies at minimum to:

- Skills skill chips (`proficiency`, `yearsOfExperience`, duplicate/name issues on
  chip-only skills),
- Projects technology chips (duplicate technology and technology-name issues).

Edge-case automated coverage is required in Part 9.4.

### 11.5 - No orphan validator errors regression test

Add a regression test suite that prevents UI/presenter drift after the summary
block is removed. Full edge-case requirements are defined in **Part 9.7**; this
section is the product requirement, Part 9 is the test specification.

At minimum, every section must participate in orphan-error coverage and combined
multi-section fixtures must be tested too.

## Part 10 - Files Expected to Change

Core:

- no validator rule changes expected, except tiny key-mapping fixes if required

UI:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`
- `src/ReVitae/Controls/ExpandableSection.axaml` (only if header hosting requires it)
- `src/ReVitae/Controls/ExpandableSection.axaml.cs` (only if header hosting requires it)
- `src/ReVitae/Ui/UiClasses.cs`
- `src/ReVitae/Themes/ReVitaeMaterialStyles.axaml`
- new shared files under `src/ReVitae/Ui/Validation/`
  - include `ValidatedDateRangeField`
  - include validation touch/interaction tracking helper
- all `*SectionView.cs` files listed above

Localization:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

Tests:

- new files under `tests/ReVitae.Tests/Ui/Validation/`
- presenter, touch-tracking, date-range, navigation, accessibility, and orphan-error
  edge case suites (Part 9)
- update any tests that referenced removed summary behavior

## Acceptance Criteria

The prompt is complete when all of the following are true:

1. `ValidationSummaryTextBlock` is removed and no equivalent full-form error dump
   exists.
2. Every validator error is rendered once, directly under its related control —
   including nested skill/project errors that currently exist only in the summary.
3. Invalid inputs are visually highlighted with a red invalid state.
4. Export failure scrolls to and reveals the first invalid field instead of only
   showing a bottom list.
5. Collapsed section/card error indicators still exist, but only as compact
   summaries, not as replacements for inline field errors.
6. Personal info and all repeatable sections use the same shared validation UI
   pattern.
7. Nested collection errors (skills/projects technologies) are not lost or only
   visible in a generic unrelated place when a specific item is invalid.
8. Light/dark theme both show readable red inline errors.
9. All existing tests pass, plus **comprehensive edge case tests** cover every
   area in Part 9 (presenter mapping, touch tracking, date-range field, chips,
   navigation, accessibility, orphan errors, summary removal).
10. Touch-aware validation display works: untouched optional/draft fields stay visually
    clean until blur, edit-after-export-failure, or failed export reveals them.
11. Invalid controls expose localized accessibility help text.
12. Shared `ValidatedDateRangeField` is used by all four date-bearing sections.
13. Invalid skill/project chips use `re-vitae-chip-invalid` and tooltip error text.
14. Orphan-error regression suite passes for single-section and combined fixtures.
15. `npm run lint` and `./scripts/test.sh` pass.

## Manual QA Checklist

Verify manually after implementation:

1. Leave required personal fields empty → red inline messages under those exact
   fields, no bottom dump.
2. Import a PDF with many incomplete work/project entries → errors appear inside
   each expanded card under date/name fields.
3. Collapse an invalid entry card → compact badge appears; expanding reveals the
   same inline messages, not new duplicates.
4. Toggle `Currently working` / `Currently active` → end-date errors disappear and
   end-date controls lose invalid styling.
5. Create duplicate link URL or duplicate language → inline error under that entry
   field.
6. Create duplicate project technology → error visible at technology UI, not only
   after scrolling to bottom.
7. Add a skill chip with invalid/missing proficiency data → inline error appears
   near the chip/skills area without any bottom summary.
8. Click Export with invalid form → first invalid field scrolls into view and
   export remains blocked; `ExportFixValidation` remains the only generic export
   status message.
9. Fix the invalid field → inline error and red border disappear immediately.
10. Switch app language → inline validation messages remain localized.
11. Fresh app / after import, untouched invalid fields stay visually quiet until blur
    or failed export reveals them.
12. Tab to an invalid field → accessibility help text announces the localized error.
13. Invalid skill/project chip shows red chip styling and tooltip error text.
14. Work Experience, Education, Projects, and Certificates all use the same shared
    date-range field error layout.

## Implementation Notes

- Prefer refactoring over rewriting section views from scratch.
- Preserve import-confidence styling (`re-vitae-import-hint`) and ensure invalid
  state can coexist predictably: invalid should override hint border while error
  remains unresolved; clearing the error should restore import hint styling if
  the field is still low-confidence and unchanged.
- Prompt 003 originally allowed a simple validation summary; this prompt
  intentionally supersedes that UX decision with strict inline-only feedback.
- Keep validation logic in Core; UI layer only presents errors.
- This prompt is intentionally front-end focused so the app feels production-ready
  before adding persistence or export improvements.

## Suggested Implementation Order

1. Add shared validation UI helper + theme invalid classes (`re-vitae-field-invalid`,
   `re-vitae-chip-invalid`).
2. Add `ValidatedDateRangeField` and validation touch/interaction tracking.
3. Remove global summary and wire personal info through helper.
4. Migrate Work Experience as the reference repeatable-section implementation.
5. Apply the same pattern to remaining sections, including chip-level invalid state
   for Skills and Projects.
6. Add section-level collapsed indicators, accessibility help text wiring, and
   export scroll-to-first-error.
7. Add presenter tests, touch-tracking tests, date-range tests, navigation tests,
   accessibility tests, orphan-error regression tests, and run lint/test scripts.

This delivers web-form-quality validation UX across the entire app while keeping
the existing deterministic Core validation system intact.
