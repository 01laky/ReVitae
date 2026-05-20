# Prompt 011 - Education

Add the next repeatable CV section: `Education`.

## Goal

Extend ReVitae with a structured education section that supports multiple
education entries, drag-and-drop reordering, validation, localization, preview
rendering, and plain PDF export.

This step should build on:

- the existing `Main / Personal information` section,
- the existing `Work Experience` repeatable section pattern,
- `ExpandableSection` and Material-styled form UI from prompts 008–010,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts.

Education should feel like a sibling feature to Work Experience, not a one-off
form block.

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Education` section,
- each individual education entry card inside the section.

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
conventions already used by Work Experience.

## Section Structure

Education should be implemented as a repeatable list of entries.

Each entry represents one qualification or study period at one institution.

The user should be able to:

- add a new education entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry card,
- sort entries by date with a dedicated action,
- edit all fields inside each entry.

The default order in the UI should place the newest entry at the top after adding,
but the user must be able to change order manually through drag-and-drop.

An empty education list is allowed. Users without formal education entries should
not be blocked from using the app.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Add your most recent qualification first. You can reorder entries later.`

### Duplicate Entry

Each education entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

### Sort By Date

Provide a localized `Sort by date (newest first)` action for the education
section.

Sorting rules:

- entries with `Currently studying here` checked should be treated as the most
  recent,
- when start dates tie, prefer the later end date,
- when end dates also tie, preserve the previous relative order,
- draft entries with no user input should remain at the bottom or be ignored by
  sorting according to one consistent rule and covered by tests,
- manual drag-and-drop order should remain available before and after sorting.

## Fields Per Entry

Each education entry should contain these fields:

### Core Fields

- institution,
- degree,
- field of study,
- location,
- degree type,
- start date,
- end date,
- currently studying here,
- grade,
- description,
- institution URL.

### Field Notes

`Institution` is the school, university, college, bootcamp, or training provider
name.

Examples:

- Comenius University in Bratislava,
- STU in Bratislava,
- Coursera,
- General Assembly.

`Degree` is the qualification title shown in the CV.

Examples:

- Bachelor of Science,
- Master of Engineering,
- High School Diploma,
- Professional Certificate.

`Field of study` is optional and captures major, specialization, or program name.

Examples:

- Computer Science,
- Business Administration,
- Graphic Design.

`Location` is optional and should accept simple values such as city/country or
`Online`. It should not require a full street address.

`Degree type` should be a selectable value with these options:

- High School,
- Associate,
- Bachelor,
- Master,
- Doctorate,
- Certificate,
- Other.

When adding a new education entry, `Degree type` should default to `Bachelor`.

`Start date` and `End date` should use month + year, not full day precision.

The UI should use dedicated month and year inputs for each date, not free-form
text. Month should be selected from a dropdown or equivalent control. Year
should be entered or selected as a numeric value.

Examples:

- `09 / 2018`,
- `06 / 2022`.

Reuse the existing month/year validation rules and UI patterns from Work
Experience where practical.

`Currently studying here` is a checkbox.

When checked:

- the end date fields should be disabled,
- preview and export should show a localized present/current label instead of an
  end date.

`Grade` is optional and should accept simple values such as GPA, classification,
or honors text.

Examples:

- 3.8 GPA,
- First Class Honours,
- Cum Laude.

`Description` should be a multi-line free-text field for thesis topic, relevant
coursework, honors, activities, or other education details.

The UI should show a live character counter for `Description` against its maximum
allowed length, for example `145 / 2000`.

`Institution URL` is optional and should accept a valid http or https URL.

## Validation Rules

Add validation for every education field using the existing C# validation
infrastructure.

Suggested initial rules:

- institution: required, maximum 160 characters,
- degree: required, maximum 160 characters,
- field of study: optional, maximum 160 characters,
- location: optional, maximum 120 characters,
- degree type: required, must be one of the supported options,
- start date: required for active entries, valid month and year,
- end date: optional when currently studying here is checked; otherwise required
  for active entries,
- currently studying here: boolean,
- grade: optional, maximum 80 characters,
- description: optional, maximum 2000 characters,
- institution URL: optional, valid http or https URL, maximum 240 characters.

Additional validation rules:

- month must be between 1 and 12,
- year must be within a sensible range such as 1950 to 2100,
- if both start date and end date are present, start date must not be after end
  date,
- an empty education list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, that entry becomes
  active and all validation rules for that entry apply,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active education entry contains validation
  errors.

Every education field must be covered by the validation schema. Do not leave any
field outside the schema.

Use stable indexed field keys for repeatable entry validation, for example:

- `education.{entryId}.institution`,
- `education.{entryId}.degree`.

The exact key format may vary, but validation must support multiple entries
without collisions and must remain inspectable from code.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for education entries in `ReVitae.Core`.

Suggested files:

- `src/ReVitae.Core/Cv/Education/EducationEntry.cs`
- `src/ReVitae.Core/Cv/Education/DegreeType.cs`
- `src/ReVitae.Core/Cv/Education/DegreeTypeExtensions.cs`
- `src/ReVitae.Core/Cv/Education/EducationFieldKeys.cs`
- `src/ReVitae.Core/Cv/Education/EducationSchema.cs`
- `src/ReVitae.Core/Cv/Education/EducationCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Education/EducationSorter.cs`

The model should support:

- stable entry identity for UI binding and drag-and-drop reordering,
- all fields listed above,
- conversion to dictionary-based validation input where useful,
- duplication from an existing entry,
- deterministic date-based sorting for active entries,
- header summary generation for collapsed cards,
- draft vs active entry detection using the same product rules as Work
  Experience.

Reuse the existing `MonthYearValue` helper from Work Experience for date
comparison/formatting unless a shared CV date helper extraction is clearly
better with minimal scope.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Education` section below the existing `Work Experience` section.

Suggested UI files:

- `src/ReVitae/Education/EducationSectionView.cs`

The UI should:

- use the existing `ExpandableSection` control,
- follow the same Material-friendly styling conventions as Work Experience,
- reuse shared UI classes from `src/ReVitae/Ui/UiClasses.cs`,
- reuse `MaterialIconFactory` for drag and validation icons,
- show one expandable card per education entry, default open,
- show a localized empty-state hint when no entries exist,
- allow adding, duplicating, removing, and drag-and-drop reordering entries,
- provide a `Sort by date (newest first)` action,
- show a live character counter for description,
- show a validation indicator on collapsed entry headers when that entry has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- disable end date when currently studying here is checked,
- keep the layout readable and responsive within the current main window.

Implement the section primarily in code-behind UI construction, matching the
Work Experience approach, rather than introducing a separate XAML form for each
field.

Removing an entry should not require a confirmation dialog in this step.

## Main Window Integration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Integration requirements:

- render `EducationSectionView` below `WorkExperienceSectionView`,
- wire `EntriesChanged` to preview, validation, and export refresh,
- include education validation in the combined form validation flow,
- block export when active education entries contain validation errors,
- pass localized strings through `ApplyLocalization()`.

## Preview

All four existing CV templates should render education when active data exists.

Add education rendering to the shared preview data model used by template
builders in `MainWindow.axaml.cs`.

Each entry should show at minimum:

- institution,
- degree,
- field of study when present,
- location when present,
- degree type when useful for the template layout,
- date range,
- grade when present,
- description when present,
- institution URL when present.

Suggested preview section label:

- `Education`

Multi-line text in `Description` should preserve line breaks in preview and PDF
export.

Draft entries with no user input should be omitted from preview and export.

If no active education entries exist, the education section should be omitted
from preview without breaking layout.

Template rendering should remain data-driven. The same education data must work
across all current templates.

Suggested placement in templates:

- after Work Experience when both sections exist,
- before contact/links sections where that produces a readable CV order.

Exact visual styling inside the CV document may remain template-specific, but all
templates must include the education content consistently.

## PDF Export

Plain PDF export should include active education entries using the current entry
order.

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
- degree type option labels,
- present/current date label,
- add/duplicate/remove/sort action labels,
- empty-state hint text,
- drag-to-reorder tooltip,
- expand/collapse entry tooltips,
- validation badge or error-count text for collapsed entries,
- preview section label,
- preview field labels if needed,
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
- disabled end-date fields remain visually clear when currently studying is
  checked.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/EducationTests.cs`

The tests should cover normal valid values and important edge cases for every
education field and collection rule.

Tests should cover at least:

- valid complete entries,
- empty education list,
- draft entries with no input ignored for validation/export,
- entry becoming active after first field input,
- required field failures,
- maximum length boundaries,
- values exactly at and over maximum length,
- invalid and valid institution URLs,
- invalid month and year values,
- end date required when not currently studying,
- end date optional when currently studying,
- start date after end date rejection,
- degree type validation,
- partially filled active entries,
- multiple active entries validated together,
- indexed field key behavior across multiple entries,
- duplicate entry copying all field values into a new identity,
- date-based sorting for newest-first order,
- currently studying entries sorting ahead of completed entries with older dates,
- draft entries behavior during sorting,
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
- month/year input UI from Work Experience,
- collection validator structure,
- draft/active entry behavior,
- preview/PDF integration patterns from Work Experience.

Do not duplicate large amounts of Work Experience UI code if a small shared helper
would reduce maintenance without over-engineering.

Keep the diff focused on Education only.

## Out of Scope

Do not implement these in this prompt:

- skills section,
- languages section,
- certificates as a separate repeatable section,
- projects section,
- coursework/transcript upload,
- diploma file attachments,
- local persistence / saved CV projects,
- template-based PDF export redesign,
- AI-generated descriptions,
- rich text or markdown editor for description,
- separate GPA calculator UI,
- cloud sync,
- import from existing CV documents,
- education-specific preview modal or new app chrome changes.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new education tests must pass.

Manual UI checks should include:

- education section visible below work experience,
- empty-state hint when no entries exist,
- add/duplicate/remove/reorder education entries,
- sort by date action,
- validation errors on fields and collapsed entry headers,
- currently studying disables end date,
- description character counter updates live,
- all four preview templates render education,
- inline preview and expanded preview modal stay in sync,
- PDF export includes education in entry order,
- export blocked when education validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support a full `Education` repeatable CV section with the same
interaction quality as Work Experience: expandable cards, drag-and-drop
reordering, duplicate and sort actions, draft/active validation behavior, live
localized validation, rendering in all four preview templates, and inclusion in
plain PDF export.

The form should remain cohesive with the existing Material-styled app shell, and
all education fields should be represented in the schema with edge-case unit test
coverage.
