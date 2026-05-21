# Prompt 015 - Projects

Add the next repeatable CV section: `Projects`.

## Goal

Extend ReVitae with a structured projects section that supports multiple project
entries, technology chips with autocomplete and bulk import, optional date ranges,
drag-and-drop reordering, optional date-based sorting, validation, localization,
preview rendering, and plain PDF export.

This step should build on:

- the existing `Main / Personal information` section,
- the repeatable section patterns from `Work Experience`, `Education`, `Skills`,
  `Languages`, and `Certificates`,
- `ExpandableSection` and Material-styled form UI from prompts 008–014,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts,
- the corrected drag-and-drop approach used by `SkillsSectionView`,
  `LanguagesSectionView`, and `CertificatesSectionView` (pointer capture on the
  entries panel with drop resolution through `PointerMoved`/`PointerReleased`
  hit testing),
- chip/tag UI, autocomplete, and bulk-add patterns from `SkillsSectionView`.

Projects should feel like a sibling feature to the other repeatable CV sections,
not a one-off form block.

This section is for **standalone portfolio or side projects** shown on the CV.
It is separate from:

- job responsibilities and achievements inside `Work Experience`,
- the dedicated `Skills` section,
- per-job `Technologies` text inside work experience entries.

Do not merge, migrate, or auto-sync data between those fields in this prompt.

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Projects` section,
- each individual project entry card inside the section.

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
conventions already used by Work Experience, Education, Skills, Languages, and
Certificates.

## Section Structure

Projects should be implemented as a repeatable list of entries.

Each entry represents one project the user wants to highlight on the CV.

The user should be able to:

- add a new project entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry card,
- sort entries by start date with a dedicated action,
- edit all fields inside each entry,
- add, remove, and bulk-add technology chips inside each entry.

The default order in the UI should place the newest entry at the top after adding,
but the user must be able to change order manually through drag-and-drop.

An empty projects list is allowed. Users without project entries should not be
blocked from using the app.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Add your strongest projects first. You can reorder entries later.`

### Duplicate Entry

Each project entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry, including nested technology items
  with new stable identities,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

### Sort By Date

Provide a localized `Sort by date (newest first)` action for the projects
section.

Sorting rules:

- sort active entries by start date, newest first,
- entries without a complete start date should sort after dated entries using one
  consistent rule and be covered by tests,
- when start dates tie, preserve the previous relative order,
- draft entries with no user input should remain at the bottom or be ignored by
  sorting according to one consistent rule and covered by tests,
- manual drag-and-drop order should remain available before and after sorting.

## Fields Per Entry

Each project entry should contain these fields:

### Core Fields

- project name,
- role,
- organization or context,
- start date,
- end date,
- currently active,
- project URL,
- technologies,
- highlights,
- description.

### Field Notes

`Project name` is the title shown in the CV.

Examples:

- ReVitae,
- E-commerce Admin Dashboard,
- Mobile Banking App Redesign.

`Role` is optional and describes the user's contribution.

Examples:

- Lead Developer,
- Full-stack Engineer,
- UX Designer,
- Personal Project.

`Organization or context` is optional and captures client, employer, school,
hackathon, or personal context when useful.

Examples:

- Personal project,
- Acme Corp,
- Comenius University,
- Hackathon project.

`Start date` and `End date` are optional and use month + year, not full day
precision.

The UI should use dedicated month and year inputs, not free-form text. Month
should be selected from a dropdown or equivalent control. Year should be entered
or selected as a numeric value.

Examples:

- `01 / 2023`,
- `09 / 2025`.

Reuse the existing month/year validation rules and UI patterns from Work
Experience, Education, and Certificates where practical.

Because project dates are often omitted on CVs, start and end dates should remain
optional even for active entries. Validation should only enforce date consistency
when the relevant date parts are provided.

`Currently active` is a checkbox.

When checked:

- the end date fields should be disabled,
- preview and export should show a localized present/current label instead of an
  end date when a start date exists.

`Project URL` is optional and should accept a valid http or https URL.

Examples:

- `https://github.com/user/revitae`,
- `https://example.com/demo`.

`Technologies` should capture tools, frameworks, languages, or platforms used in
the project.

Implement technologies as **chip/tag items inside the project entry**, similar to
skill chips in `SkillsSectionView`.

Each technology chip should:

- show the technology name,
- provide a remove action,
- be addable through autocomplete input,
- be addable in bulk from comma- or newline-separated text.

Reuse the existing static skill suggestion list from `SkillsSuggestions` for
autocomplete where practical. The user must still be able to enter custom
technology names not present in the suggestion list.

Within one project entry, duplicate technology names must fail validation using
case-insensitive comparison.

`Highlights` should be a separate optional multi-line field for measurable
results, impact, or key outcomes.

Examples:

- reduced build time by 40%,
- shipped MVP in 6 weeks,
- won 2nd place at university hackathon.

`Description` should be a multi-line free-text field for project summary,
responsibilities, architecture notes, or feature overview.

The UI should show a live character counter for `Highlights` and `Description`
against their maximum allowed lengths.

Examples:

- Built a cross-platform desktop CV editor with live preview and PDF export,
- Designed the data model to separate CV content from template rendering.

## Validation Rules

Add validation for every project field using the existing C# validation
infrastructure.

Suggested initial rules:

- project name: required for active entries, maximum 160 characters,
- role: optional, maximum 120 characters,
- organization or context: optional, maximum 160 characters,
- start month: optional, valid month when provided,
- start year: optional, valid year when provided,
- end month: optional, valid month when provided unless currently active is
  checked,
- end year: optional, valid year when provided unless currently active is
  checked,
- currently active: boolean,
- project URL: optional, valid http or https URL, maximum 240 characters,
- technology name: required for each active technology item, maximum 80
  characters,
- highlights: optional, maximum 2000 characters,
- description: optional, maximum 2000 characters,
- bulk technologies text: maximum 1000 characters in the UI.

Additional validation rules:

- month must be between 1 and 12 when provided,
- year must be within a sensible range such as 1950 to 2100 when provided,
- if both start date and end date are complete and currently active is not
  checked, start date must not be after end date,
- an empty projects list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, adds any technology
  chip, or toggles currently active, that entry becomes active and all validation
  rules for that entry apply,
- duplicate technology names within the same project entry must fail validation,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active project entry contains validation
  errors,
- whitespace-only required values inside active entries must fail validation.

Every project field must be covered by the validation schema. Do not leave any
field outside the schema.

Use stable indexed field keys for repeatable entry validation, for example:

- `projects.{entryId}.name`,
- `projects.{entryId}.role`,
- `projects.{entryId}.{technologyId}.name`,
- `projects.{entryId}.bulkTechnologies`.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for project entries and nested technology items in
`ReVitae.Core`.

Suggested files:

- `src/ReVitae.Core/Cv/Projects/ProjectTechnologyItem.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectEntry.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectsFieldKeys.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectsSchema.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectsCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectSorter.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectPreviewFormatter.cs`
- `src/ReVitae.Core/Cv/Projects/ProjectTechnologiesParser.cs`

The model should support:

- stable entry identity for UI binding and drag-and-drop reordering,
- stable technology item identity inside each entry,
- all fields listed above,
- conversion to dictionary-based validation input where useful,
- duplication from an existing entry including nested technology items,
- parsing bulk technology text into ordered technology names,
- start-date sorting,
- header summary generation for collapsed cards,
- draft vs active entry detection using the same product rules as Work
  Experience, Education, Skills, Languages, and Certificates.

Suggested draft/active behavior:

- default empty entry is a draft,
- any non-whitespace value in any scalar field, any technology item, any date
  value, or currently active toggled on makes the entry active.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Projects` section below the existing `Certificates` section.

Suggested UI files:

- `src/ReVitae/Projects/ProjectsSectionView.cs`

The UI should:

- use the existing `ExpandableSection` control,
- follow the same Material-friendly styling conventions as the other repeatable
  sections,
- reuse shared UI classes from `src/ReVitae/Ui/UiClasses.cs`,
- reuse `MaterialIconFactory` for drag and validation icons,
- show one expandable card per project entry, default open,
- show a localized empty-state hint when no entries exist,
- allow adding, duplicating, removing, drag-and-drop reordering, and sort-by-date
  actions,
- show month/year controls for optional start and end dates,
- show a currently active checkbox that disables end date inputs,
- render technology chips/tags with remove buttons inside each entry,
- provide autocomplete on the technology-name input,
- provide bulk add from comma/newline technology text,
- show a live character counter for bulk technology text,
- show live character counters for highlights and description,
- show a validation indicator on collapsed entry headers when that entry has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- keep the layout readable and responsive within the current main window.

Implement drag-and-drop using the same working pattern as `SkillsSectionView`,
`LanguagesSectionView`, and `CertificatesSectionView`:

- capture the pointer on the shared entries panel, not on the small drag handle,
- resolve the drop target during `PointerMoved`,
- apply the reorder on `PointerReleased`.

Do not reintroduce drop handling that depends on `PointerEntered` on other cards
while pointer capture is held on the drag handle.

Implement the section primarily in code-behind UI construction, matching the
other repeatable sections, rather than introducing a separate XAML form for each
field.

Removing an entry should not require a confirmation dialog in this step.

Removing a technology chip should not require a confirmation dialog in this step.

## Main Window Integration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Integration requirements:

- render `ProjectsSectionView` below `CertificatesSectionView`,
- wire `EntriesChanged` to preview, validation, and export refresh,
- include projects validation in the combined form validation flow,
- block export when active project entries contain validation errors,
- pass localized strings through `ApplyLocalization()`.

## Preview

All four existing CV templates should render projects when active data exists.

Add projects rendering to the shared preview data model used by template builders
in `MainWindow.axaml.cs`.

Each active entry should show at minimum:

- project name,
- role when present,
- organization or context when present,
- date range when start date or currently active/end date information exists,
- project URL when present,
- technologies when present,
- highlights when present,
- description when present.

Suggested preview formats:

- `ReVitae · Lead Developer · Personal project · Jan 2024 – Present`,
- `E-commerce Admin Dashboard · Full-stack Engineer · Acme Corp · Sep 2022 – Jun 2023`,
- technologies line, for example `Technologies: C#, Avalonia, PostgreSQL`,
- highlights and description as separate paragraphs or lines below the main line.

When only a project name and description exist, preview should still render a
readable block without forcing artificial date text.

Draft entries with no user input should be omitted from preview and export.

If no active project entries exist, the projects section should be omitted from
preview without breaking layout.

Template rendering should remain data-driven. The same projects data must work
across all current templates.

Suggested placement in templates:

- after Certificates when both sections exist,
- before contact/links sections where that produces a readable CV order.

Exact visual styling inside the CV document may remain template-specific, but all
templates must include the projects content consistently.

## PDF Export

Plain PDF export should include active project entries using the current entry
order.

Each exported entry should use the same text formatting rules as preview,
including technology lines and optional highlights/description blocks.

The export should remain lightweight:

- no template-based PDF design yet,
- no screenshots or embedded images required,
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
- add-technology and bulk-add labels,
- remove-technology action labels,
- empty-state hint text,
- drag-to-reorder tooltip,
- expand/collapse entry tooltips,
- validation badge or error-count text for collapsed entries,
- preview section label,
- preview labels for technologies, highlights, and present/current date text,
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
- month/year inputs, autocomplete, and chip remove buttons remain usable with
  keyboard input where practical.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/ProjectsTests.cs`

The tests should cover normal valid values and important edge cases for every
project field and collection rule.

Tests should cover at least:

- valid complete entries,
- empty projects list,
- draft entries with no input ignored for validation/export,
- entry becoming active after first field input,
- entry becoming active after adding first technology chip,
- required field failures,
- maximum length boundaries,
- values exactly at and over maximum length,
- invalid and valid month/year values when provided,
- start date after end date rejection,
- currently active allowing missing end date,
- invalid and valid project URL values when provided,
- duplicate technology names within the same entry,
- bulk technology parsing from comma and newline input,
- partially filled active entries,
- multiple active entries validated together,
- indexed field key behavior across multiple entries and nested technology items,
- duplicate entry copying all scalar and nested technology values with new
  identities,
- sort-by-date newest-first behavior including entries without start dates,
- draft entry handling during sorting,
- preview formatting with and without role, dates, technologies, highlights, and
  description,
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
- month/year field UI and validation from Work Experience, Education, and
  Certificates,
- date sorting patterns from Certificates and Education,
- chip/tag UI patterns from `SkillsSectionView`,
- technology parsing patterns from `SkillsTextParser` where practical,
- static autocomplete suggestions from `SkillsSuggestions`,
- drag-and-drop pattern from `SkillsSectionView` / `LanguagesSectionView` /
  `CertificatesSectionView`,
- preview/PDF integration patterns from Work Experience, Education, Skills,
  Languages, and Certificates.

Do not duplicate large amounts of existing repeatable-section UI code if a small
shared helper would reduce maintenance without over-engineering.

Keep the diff focused on Projects only.

## Out of Scope

Do not implement these in this prompt:

- local persistence / saved CV projects,
- importing projects automatically from Work Experience descriptions,
- deduplicating project technologies against Skills or Work Experience
  technologies during editing,
- screenshots, thumbnails, or gallery images for projects,
- repository star counts or live GitHub API integration,
- template-based PDF export redesign,
- AI-generated project descriptions or technology suggestions,
- rich text or markdown editor for highlights/description,
- cloud sync,
- import from existing CV documents,
- projects-specific preview modal or new app chrome changes,
- links or additional-information sections as separate form blocks,
- drag-and-drop reordering of technology chips inside an entry (manual chip order
  from add/bulk-add order is sufficient in this step).

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new projects tests must pass.

Manual UI checks should include:

- projects section visible below certificates,
- empty-state hint when no entries exist,
- add/duplicate/remove/reorder project entries,
- drag-and-drop reorder works while holding the mouse button and releasing over
  another entry,
- sort by date places newest start dates first,
- optional dates validate correctly when provided,
- currently active disables end date and shows present label in preview,
- add/remove technology chips,
- autocomplete suggestions while typing technology names,
- bulk add from comma/newline text,
- bulk technology character counter updates live,
- highlights and description character counters update live,
- validation errors on fields, technology inputs, and collapsed entry headers,
- all four preview templates render projects,
- inline preview and expanded preview modal stay in sync,
- PDF export includes projects in entry order,
- export blocked when projects validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support a full `Projects` repeatable CV section with expandable
cards, technology chips with autocomplete and bulk import, optional date ranges,
drag-and-drop reordering, sort-by-date, duplicate actions, draft/active validation
behavior, live localized validation, rendering in all four preview templates, and
inclusion in plain PDF export.

The form should remain cohesive with the existing Material-styled app shell, and
all project fields should be represented in the schema with edge-case unit test
coverage.

After this prompt, the Phase 1 structured CV form will contain all primary
repeatable content sections from the product concept except standalone
`Links / additional information` blocks and persistence.
