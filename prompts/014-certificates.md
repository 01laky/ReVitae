# Prompt 014 - Certificates

Add the next repeatable CV section: `Certificates`.

## Goal

Extend ReVitae with a structured certificates section that supports multiple
professional certification entries, drag-and-drop reordering, optional date-based
sorting, validation, localization, preview rendering, and plain PDF export.

This step should build on:

- the existing `Main / Personal information` section,
- the repeatable section patterns from `Work Experience`, `Education`, `Skills`,
  and `Languages`,
- `ExpandableSection` and Material-styled form UI from prompts 008–013,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts,
- the corrected drag-and-drop approach used by `SkillsSectionView` and
  `LanguagesSectionView` (pointer capture on the entries panel with drop
  resolution through `PointerMoved`/`PointerReleased` hit testing).

Certificates should feel like a sibling feature to the other repeatable CV
sections, not a one-off form block.

This section is for **standalone professional certifications** such as cloud,
security, language, or vendor credentials. It is separate from:

- the optional `Certificate or note` field inside a `Languages` entry,
- the `Certificate` degree type inside `Education`.

Do not merge or migrate data between those fields in this prompt.

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Certificates` section,
- each individual certificate entry card inside the section.

Requirements:

- default state for every expandable section is **open/expanded**,
- collapsing a section must not hide, clear, or disable validation,
- collapsed entry headers must show a short readable summary,
- collapsed entry headers must show a validation indicator when that entry has
  errors,
- drag-and-drop reordering must still work when entries are expanded or
  collapsed,
- drag handle and expand/collapse controls must not conflict.

Use the existing `ExpandableSection` control and the same header/action layout
conventions already used by Work Experience, Education, Skills, and Languages.

## Section Structure

Certificates should be implemented as a repeatable list of entries.

Each entry represents one professional certification or credential shown on the
CV.

The user should be able to:

- add a new certificate entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry card,
- sort entries by issue date with a dedicated action,
- edit all fields inside each entry.

The default order in the UI should place the newest entry at the top after adding,
but the user must be able to change order manually through drag-and-drop.

An empty certificates list is allowed. Users without certification entries should
not be blocked from using the app.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Add your most relevant certifications first. You can reorder entries later.`

### Duplicate Entry

Each certificate entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

### Sort By Date

Provide a localized `Sort by date (newest first)` action for the certificates
section.

Sorting rules:

- sort by issue date, newest first,
- when issue dates tie, preserve the previous relative order,
- draft entries with no user input should remain at the bottom or be ignored by
  sorting according to one consistent rule and covered by tests,
- manual drag-and-drop order should remain available before and after sorting.

## Fields Per Entry

Each certificate entry should contain these fields:

### Core Fields

- certificate name,
- issuing organization,
- issue date,
- expiration date,
- credential ID,
- credential URL,
- description or note.

### Field Notes

`Certificate name` is the credential title shown in the CV.

Examples:

- AWS Certified Solutions Architect – Associate,
- Microsoft Certified: Azure Administrator Associate,
- Certified Scrum Master (CSM),
- IELTS Academic 8.0.

`Issuing organization` is the issuer or certification body.

Examples:

- Amazon Web Services,
- Microsoft,
- Scrum Alliance,
- British Council.

The UI may optionally provide autocomplete suggestions for common issuers from a
static in-app list, while still allowing custom issuer names.

`Issue date` should use month + year, not full day precision.

The UI should use dedicated month and year inputs, not free-form text. Month
should be selected from a dropdown or equivalent control. Year should be entered
or selected as a numeric value.

Examples:

- `03 / 2024`,
- `11 / 2021`.

Reuse the existing month/year validation rules and UI patterns from Work
Experience and Education where practical.

`Expiration date` is optional and uses the same month + year inputs.

When provided, preview and export should show a localized expiration label,
for example:

- `Valid until 06 / 2027`.

When expiration date is not set, omit expiration text from preview and export.

`Credential ID` is optional and should accept short verification identifiers.

Examples:

- ABC123456789,
- Verification code: 4A7B-91C2.

`Credential URL` is optional and should accept a valid http or https URL for
online verification or badge pages.

Examples:

- `https://www.credly.com/badges/...`,
- `https://learn.microsoft.com/.../credentials/...`.

`Description or note` is optional and should accept short supporting text such as
exam score, specialization, or renewal context.

Examples:

- Score: 920 / 1000,
- Renewed annually,
- Specialty: Security.

This field is for short notes, not long descriptions. Use a multi-line input with
a live character counter against its maximum allowed length.

## Validation Rules

Add validation for every certificate field using the existing C# validation
infrastructure.

Suggested initial rules:

- certificate name: required for active entries, maximum 160 characters,
- issuing organization: required for active entries, maximum 160 characters,
- issue date: required for active entries, valid month and year,
- expiration date: optional, valid month and year when provided,
- credential ID: optional, maximum 80 characters,
- credential URL: optional, valid http or https URL, maximum 240 characters,
- description or note: optional, maximum 500 characters.

Additional validation rules:

- month must be between 1 and 12,
- year must be within a sensible range such as 1950 to 2100,
- if both issue date and expiration date are present, issue date must not be
  after expiration date,
- an empty certificates list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, that entry becomes
  active and all validation rules for that entry apply,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active certificate entry contains validation
  errors,
- whitespace-only required values inside active entries must fail validation.

Every certificate field must be covered by the validation schema. Do not leave
any field outside the schema.

Use stable indexed field keys for repeatable entry validation, for example:

- `certificates.{entryId}.name`,
- `certificates.{entryId}.issuer`,
- `certificates.{entryId}.issueMonth`.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for certificate entries in `ReVitae.Core`.

Suggested files:

- `src/ReVitae.Core/Cv/Certificates/CertificateEntry.cs`
- `src/ReVitae.Core/Cv/Certificates/CertificatesFieldKeys.cs`
- `src/ReVitae.Core/Cv/Certificates/CertificatesSchema.cs`
- `src/ReVitae.Core/Cv/Certificates/CertificatesCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Certificates/CertificateSorter.cs`
- `src/ReVitae.Core/Cv/Certificates/CertificatePreviewFormatter.cs`
- `src/ReVitae.Core/Cv/Certificates/IssuerSuggestions.cs` (optional, if
  autocomplete is implemented)

The model should support:

- stable entry identity for UI binding and drag-and-drop reordering,
- all fields listed above,
- conversion to dictionary-based validation input where useful,
- duplication from an existing entry,
- issue-date sorting,
- header summary generation for collapsed cards,
- draft vs active entry detection using the same product rules as Work
  Experience, Education, Skills, and Languages.

Suggested draft/active behavior:

- default empty entry is a draft,
- any non-whitespace value in any field makes the entry active.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Certificates` section below the existing `Languages` section.

Suggested UI files:

- `src/ReVitae/Certificates/CertificatesSectionView.cs`

The UI should:

- use the existing `ExpandableSection` control,
- follow the same Material-friendly styling conventions as the other repeatable
  sections,
- reuse shared UI classes from `src/ReVitae/Ui/UiClasses.cs`,
- reuse `MaterialIconFactory` for drag and validation icons,
- show one expandable card per certificate entry, default open,
- show a localized empty-state hint when no entries exist,
- allow adding, duplicating, removing, drag-and-drop reordering, and sort-by-date
  actions,
- optionally provide autocomplete on the issuing organization input,
- show month/year controls for issue and expiration dates,
- show a live character counter for the description field,
- show a validation indicator on collapsed entry headers when that entry has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- keep the layout readable and responsive within the current main window.

Implement drag-and-drop using the same working pattern as `SkillsSectionView`
and `LanguagesSectionView`:

- capture the pointer on the shared entries panel, not on the small drag handle,
- resolve the drop target during `PointerMoved`,
- apply the reorder on `PointerReleased`.

Do not reintroduce drop handling that depends on `PointerEntered` on other cards
while pointer capture is held on the drag handle.

Implement the section primarily in code-behind UI construction, matching the
other repeatable sections, rather than introducing a separate XAML form for each
field.

Removing an entry should not require a confirmation dialog in this step.

## Main Window Integration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Integration requirements:

- render `CertificatesSectionView` below `LanguagesSectionView`,
- wire `EntriesChanged` to preview, validation, and export refresh,
- include certificates validation in the combined form validation flow,
- block export when active certificate entries contain validation errors,
- pass localized strings through `ApplyLocalization()`.

## Preview

All four existing CV templates should render certificates when active data exists.

Add certificates rendering to the shared preview data model used by template
builders in `MainWindow.axaml.cs`.

Each active entry should show at minimum:

- certificate name,
- issuing organization,
- issue date,
- expiration date when present,
- credential ID when present,
- credential URL when present,
- description or note when present.

Suggested preview formats:

- `AWS Certified Solutions Architect – Associate · Amazon Web Services · Mar 2024`,
- `Microsoft Certified: Azure Administrator Associate · Microsoft · Nov 2021 · Valid until Jun 2026`,
- include credential ID or URL on a secondary line when present, for example
  `Credential ID: ABC123456789`.

Suggested preview section label:

- `Certificates`

Draft entries with no user input should be omitted from preview and export.

If no active certificate entries exist, the certificates section should be
omitted from preview without breaking layout.

Template rendering should remain data-driven. The same certificates data must
work across all current templates.

Suggested placement in templates:

- after Languages when both sections exist,
- before contact/links sections where that produces a readable CV order.

Exact visual styling inside the CV document may remain template-specific, but all
templates must include the certificates content consistently.

## PDF Export

Plain PDF export should include active certificate entries using the current
entry order.

Each exported entry should use the same text formatting rules as preview.

The export should remain lightweight:

- no template-based PDF design yet,
- no badge images required,
- no colors required,
- only structured text from the form.

If validation fails, export must remain blocked and show the existing localized
validation feedback pattern.

## Internationalization

Add translation keys for all new user-facing text.

This includes:

- section title,
- field labels,
- placeholders where practical,
- add/duplicate/remove/sort action labels,
- empty-state hint text,
- drag-to-reorder tooltip,
- expand/collapse entry tooltips,
- validation badge or error-count text for collapsed entries,
- preview section label,
- expiration date preview label,
- all validation messages.

Add keys to:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

Every supported language must receive the new required translation keys.

Do not hardcode new UI strings in XAML or code-behind.

## Accessibility

Preserve or improve accessibility:

- localized tooltips for drag, expand, and collapse actions,
- meaningful automation names where practical,
- validation errors remain readable and visually associated with fields,
- month/year inputs and optional autocomplete remain usable with keyboard input.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/CertificatesTests.cs`

The tests should cover normal valid values and important edge cases for every
certificate field and collection rule.

Tests should cover at least:

- valid complete entries,
- empty certificates list,
- draft entries with no input ignored for validation/export,
- entry becoming active after first field input,
- required field failures,
- maximum length boundaries,
- values exactly at and over maximum length,
- invalid and valid month/year values,
- issue date after expiration date rejection,
- invalid and valid credential URL values when provided,
- partially filled active entries,
- multiple active entries validated together,
- indexed field key behavior across multiple entries,
- duplicate entry copying all field values into a new identity,
- sort-by-date newest-first behavior including tied issue dates,
- draft entry handling during sorting,
- optional issuer autocomplete filtering when implemented,
- preview formatting with and without expiration date,
- translation key usage in schema messages,
- whitespace-only required values inside active entries,
- header summary generation for collapsed cards.

Tests should run through the existing C# lint/test flow.

## Code Reuse Rules

Prefer extending existing patterns over inventing new ones.

Reuse where practical:

- `ExpandableSection`,
- `UiClasses`,
- `MaterialIconFactory`,
- collection validator structure,
- draft/active entry behavior,
- month/year field UI and validation from Work Experience and Education,
- date sorting patterns from Education,
- drag-and-drop pattern from `SkillsSectionView` / `LanguagesSectionView`,
- preview/PDF integration patterns from Work Experience, Education, Skills, and
  Languages.

Do not duplicate large amounts of existing repeatable-section UI code if a small
shared helper would reduce maintenance without over-engineering.

Keep the diff focused on Certificates only.

## Out of Scope

Do not implement these in this prompt:

- projects section,
- badge images, PDF embeds, or QR codes for credentials,
- automatic verification against external certificate APIs,
- merging certificates with Education degree type `Certificate`,
- migrating or syncing the Languages `Certificate or note` field,
- local persistence / saved CV projects,
- template-based PDF export redesign,
- AI-generated certificate suggestions,
- rich text or markdown editor for notes,
- cloud sync,
- import from existing CV documents,
- certificates-specific preview modal or new app chrome changes.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new certificates tests must pass.

Manual UI checks should include:

- certificates section visible below languages,
- empty-state hint when no entries exist,
- add/duplicate/remove/reorder certificate entries,
- drag-and-drop reorder works while holding the mouse button and releasing over
  another entry,
- sort by date places newest issue dates first,
- month/year inputs validate correctly,
- optional expiration date shown in preview when set,
- validation errors on fields and collapsed entry headers,
- all four preview templates render certificates,
- inline preview and expanded preview modal stay in sync,
- PDF export includes certificates in entry order,
- export blocked when certificates validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support a full `Certificates` repeatable CV section with
expandable cards, drag-and-drop reordering, sort-by-date, duplicate actions,
draft/active validation behavior, live localized validation, rendering in all
four preview templates, and inclusion in plain PDF export.

The form should remain cohesive with the existing Material-styled app shell, and
all certificate fields should be represented in the schema with edge-case unit
test coverage.
