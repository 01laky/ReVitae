# Prompt 013 - Languages

Add the next repeatable CV section: `Languages`.

## Goal

Extend ReVitae with a structured languages section that supports multiple
language entries, drag-and-drop reordering, validation, localization, preview
rendering, and plain PDF export.

This step should build on:

- the existing `Main / Personal information` section,
- the repeatable section patterns from `Work Experience`, `Education`, and
  `Skills`,
- `ExpandableSection` and Material-styled form UI from prompts 008–012,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts,
- the corrected drag-and-drop approach used by `SkillsSectionView` (pointer
  capture on the entries panel with drop resolution through
  `PointerMoved`/`PointerReleased` hit testing).

Languages should feel like a sibling feature to the other repeatable CV
sections, not a one-off form block.

Unlike Work Experience and Education, language entries do **not** use dates.
Ordering is manual through drag-and-drop only.

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Languages` section,
- each individual language entry card inside the section.

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
conventions already used by Work Experience, Education, and Skills.

## Section Structure

Languages should be implemented as a repeatable list of entries.

Each entry represents one spoken/written language and its proficiency on the CV.

The user should be able to:

- add a new language entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry card,
- edit all fields inside each entry.

There is **no sort-by-date action** in this section. Date-based sorting does not
apply to languages.

Manual drag-and-drop order is the source of truth for preview and export.

An empty languages list is allowed. Users without a dedicated languages section
should not be blocked from using the app.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Add your strongest languages first. You can reorder entries later.`

### Duplicate Entry

Each language entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

## Fields Per Entry

Each language entry should contain these fields:

### Core Fields

- language,
- proficiency,
- CEFR level,
- certificate or note.

### Optional Sub-Skill Fields

Each entry may optionally specify separate proficiency levels for:

- reading,
- writing,
- speaking,
- listening.

These sub-skills reuse the same proficiency enum as the main proficiency field
(Elementary through Native). All four are optional. When set, preview and export
should show them as indented or secondary lines below the main language line,
for example:

- `Reading: Advanced`
- `Writing: Intermediate`
- `Speaking: Fluent`
- `Listening: Advanced`

When a sub-skill is unset, omit it from preview and export.

### Language Flags

Show a flag emoji next to the language name in the UI entry header and in
preview/export when the language matches a known name from a static in-app map.

Examples:

- English → 🇬🇧,
- Slovak → 🇸🇰,
- German → 🇩🇪.

Use emoji flags only (no image assets). Unknown or custom language names should
fall back to a neutral globe emoji (🌐) or no flag when the name does not match.

Flag resolution must be case-insensitive on the language name.

### Field Notes

`Language` is the language name shown in the CV.

Examples:

- English,
- Slovak,
- German,
- Spanish.

The UI should provide autocomplete suggestions for common languages from a
static in-app list, while still allowing custom language names.

`Proficiency` should be a selectable value with these options:

- Elementary,
- Intermediate,
- Advanced,
- Fluent,
- Native.

When adding a new language entry, `Proficiency` should default to `Intermediate`.

`CEFR level` is optional and should use these options:

- A1,
- A2,
- B1,
- B2,
- C1,
- C2.

The UI should allow leaving CEFR unset when the user does not want to show a
framework level.

When CEFR is set, preview and export should include it alongside proficiency,
for example:

- `English · Fluent · C1`

When CEFR is not set, preview should show only language and proficiency, for
example:

- `Slovak · Native`

`Certificate or note` is optional and should accept short qualification text.

Examples:

- IELTS 8.0,
- DELE B2,
- FCE.

This field is for credentials or short notes, not long descriptions.

## Validation Rules

Add validation for every language field using the existing C# validation
infrastructure.

Suggested initial rules:

- language: required for active entries, maximum 80 characters,
- proficiency: required for active entries, must be one of the supported options,
- CEFR level: optional, must be one of the supported options when provided,
- certificate or note: optional, maximum 120 characters,
- reading / writing / speaking / listening: optional, each must be one of the
  supported proficiency options when provided.

Additional validation rules:

- an empty languages list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, that entry becomes
  active and all validation rules for that entry apply,
- duplicate language names across active entries must fail validation using
  case-insensitive comparison,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active language entry contains validation
  errors,
- whitespace-only required values inside active entries must fail validation.

Every language field must be covered by the validation schema. Do not leave any
field outside the schema.

Use stable indexed field keys for repeatable entry validation, for example:

- `languages.{entryId}.language`,
- `languages.{entryId}.proficiency`.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for language entries in `ReVitae.Core`.

Suggested files:

- `src/ReVitae.Core/Cv/Languages/LanguageEntry.cs`
- `src/ReVitae.Core/Cv/Languages/LanguageProficiency.cs`
- `src/ReVitae.Core/Cv/Languages/LanguageProficiencyExtensions.cs`
- `src/ReVitae.Core/Cv/Languages/CefrLevel.cs`
- `src/ReVitae.Core/Cv/Languages/CefrLevelExtensions.cs`
- `src/ReVitae.Core/Cv/Languages/LanguagesFieldKeys.cs`
- `src/ReVitae.Core/Cv/Languages/LanguagesSchema.cs`
- `src/ReVitae.Core/Cv/Languages/LanguagesCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Languages/LanguageSuggestions.cs`
- `src/ReVitae.Core/Cv/Languages/LanguageFlagResolver.cs`
- `src/ReVitae.Core/Cv/Languages/LanguagePreviewFormatter.cs`

The model should support:

- stable entry identity for UI binding and drag-and-drop reordering,
- all fields listed above including optional sub-skills,
- flag emoji resolution for known language names,
- conversion to dictionary-based validation input where useful,
- duplication from an existing entry,
- autocomplete filtering from a static suggestion list,
- header summary generation for collapsed cards,
- draft vs active entry detection using the same product rules as Work
  Experience, Education, and Skills.

Suggested draft/active behavior:

- default empty entry is a draft,
- any non-whitespace value in any field makes the entry active.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Languages` section below the existing `Skills` section.

Suggested UI files:

- `src/ReVitae/Languages/LanguagesSectionView.cs`

The UI should:

- use the existing `ExpandableSection` control,
- follow the same Material-friendly styling conventions as the other repeatable
  sections,
- reuse shared UI classes from `src/ReVitae/Ui/UiClasses.cs`,
- reuse `MaterialIconFactory` for drag and validation icons,
- show one expandable card per language entry, default open,
- show a localized empty-state hint when no entries exist,
- allow adding, duplicating, removing, and drag-and-drop reordering entries,
- provide autocomplete on the language-name input,
- show a flag emoji beside the language name in entry headers when resolvable,
- provide optional combo boxes for reading, writing, speaking, and listening
  sub-skills,
- show a validation indicator on collapsed entry headers when that entry has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- keep the layout readable and responsive within the current main window.

Implement drag-and-drop using the same working pattern as `SkillsSectionView`:

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

- render `LanguagesSectionView` below `SkillsSectionView`,
- wire `EntriesChanged` to preview, validation, and export refresh,
- include languages validation in the combined form validation flow,
- block export when active language entries contain validation errors,
- pass localized strings through `ApplyLocalization()`.

## Preview

All four existing CV templates should render languages when active data exists.

Add languages rendering to the shared preview data model used by template
builders in `MainWindow.axaml.cs`.

Each active entry should show at minimum:

- language with flag emoji when resolvable,
- proficiency label,
- CEFR level when present,
- certificate or note when present,
- sub-skill lines for reading, writing, speaking, and listening when each is set.

Suggested preview formats:

- `🇬🇧 English · Fluent · C1`,
- `🇸🇰 Slovak · Native`,
- `🇩🇪 German · Intermediate · B1 · Goethe-Zertifikat B1` when certificate is present,
- sub-skill lines below the main line, for example `Reading: Advanced`.

Suggested preview section label:

- `Languages`

Draft entries with no user input should be omitted from preview and export.

If no active language entries exist, the languages section should be omitted from
preview without breaking layout.

Template rendering should remain data-driven. The same languages data must work
across all current templates.

Suggested placement in templates:

- after Skills when both sections exist,
- before contact/links sections where that produces a readable CV order.

Exact visual styling inside the CV document may remain template-specific, but all
templates must include the languages content consistently.

## PDF Export

Plain PDF export should include active language entries using the current entry
order.

Each exported entry should use the same text formatting rules as preview, including
flag emoji and sub-skill lines when present.

The export should remain lightweight:

- no template-based PDF design yet,
- no images required,
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
- proficiency option labels,
- CEFR option labels,
- sub-skill field labels (reading, writing, speaking, listening),
- preview sub-skill line labels,
- add/duplicate/remove action labels,
- empty-state hint text,
- drag-to-reorder tooltip,
- expand/collapse entry tooltips,
- validation badge or error-count text for collapsed entries,
- preview section label,
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
- autocomplete and combo boxes remain usable with keyboard input.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/LanguagesTests.cs`

The tests should cover normal valid values and important edge cases for every
language field and collection rule.

Tests should cover at least:

- valid complete entries,
- empty languages list,
- draft entries with no input ignored for validation/export,
- entry becoming active after first field input,
- required field failures,
- maximum length boundaries,
- values exactly at and over maximum length,
- invalid and valid proficiency values,
- invalid and valid CEFR values when provided,
- invalid and valid sub-skill proficiency values when provided,
- duplicate language names across active entries,
- partially filled active entries,
- multiple active entries validated together,
- indexed field key behavior across multiple entries,
- duplicate entry copying all field values into a new identity,
- autocomplete suggestion filtering,
- flag emoji resolution for known and unknown language names,
- sub-skill preview line formatting,
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
- drag-and-drop pattern from `SkillsSectionView`,
- preview/PDF integration patterns from Work Experience, Education, and Skills.

Do not duplicate large amounts of existing repeatable-section UI code if a small
shared helper would reduce maintenance without over-engineering.

Keep the diff focused on Languages only.

## Out of Scope

Do not implement these in this prompt:

- certificates as a separate repeatable CV section,
- projects section,
- local persistence / saved CV projects,
- template-based PDF export redesign,
- AI-generated language suggestions,
- rich text or markdown editor for notes,
- cloud sync,
- import from existing CV documents,
- languages-specific preview modal or new app chrome changes,
- custom flag image assets or SVG icon sets (emoji flags only).

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new languages tests must pass.

Manual UI checks should include:

- languages section visible below skills,
- empty-state hint when no entries exist,
- add/duplicate/remove/reorder language entries,
- drag-and-drop reorder works while holding the mouse button and releasing over
  another entry,
- autocomplete suggestions while typing language names,
- flag emoji visible in entry headers and preview for known languages,
- optional sub-skill fields (reading, writing, speaking, listening),
- validation errors on fields and collapsed entry headers,
- all four preview templates render languages,
- inline preview and expanded preview modal stay in sync,
- PDF export includes languages in entry order,
- export blocked when languages validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support a full `Languages` repeatable CV section with expandable
cards, drag-and-drop reordering, duplicate actions, optional reading/writing/
speaking/listening sub-skills, emoji flag display for known languages, draft/active
validation behavior, live localized validation, rendering in all four preview
templates, and inclusion in plain PDF export.

The form should remain cohesive with the existing Material-styled app shell, and
all language fields should be represented in the schema with edge-case unit test
coverage.
