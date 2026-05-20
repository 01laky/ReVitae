# Prompt 008 - Work Experience

Add the first repeatable CV section: `Work Experience`.

## Goal

Extend ReVitae with a structured work experience section that supports multiple
job entries, drag-and-drop reordering, validation, localization, preview rendering,
and plain PDF export.

This step should build on the existing main personal information section,
validation infrastructure, template preview system, and internationalization
layer from previous prompts.

It should also introduce a reusable expandable section pattern for the CV form.

## Expandable Sections

CV form sections should be expandable and collapsible.

This applies to:

- top-level form sections such as `Main / Personal information` and
  `Work Experience`,
- dynamic entry subsections inside repeatable sections, such as each individual
  work experience card.

Default state for every expandable section should be **open/expanded**.

The user should be able to collapse and expand:

- a whole main section to reduce visual clutter while editing other parts of the
  CV,
- a single work experience entry while keeping other entries visible.

Expanded/collapsed state is a UI concern only. Collapsing a section must not hide,
clear, or disable validation for the fields inside it.

Each expandable section should have a clear header/title area that toggles open
and closed.

When a dynamic work experience entry is collapsed, its header should still show a
short summary so the user can identify it, for example:

- job title,
- company,
- date range or present/current label.

If an active entry contains validation errors, its collapsed header should also
show a clear validation indicator, such as an error badge, icon, or count. The
user should be able to notice invalid entries without expanding every card.

Drag-and-drop reordering for work experience entries should still work when
entries are expanded or collapsed.

Define drag and expand interactions so they do not conflict. For example, use a
dedicated drag area on the entry card/header and a separate expand/collapse
toggle. Dragging must not accidentally open or close the section.

Apply the same expandable section pattern to the existing
`Main / Personal information` section so the form uses one consistent interaction
model across static and repeatable sections.

## Section Structure

Work Experience should be implemented as a repeatable list of entries.

Each entry represents one job or role at one company.

The user should be able to:

- add a new work experience entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry section,
- sort entries by date with a dedicated action,
- edit all fields inside each entry.

The default order in the UI should place the newest entry at the top after adding,
but the user must be able to change order manually through drag-and-drop.

Drag-and-drop reordering should apply to the whole entry card/section, not only
to a small handle icon. The interaction should feel clear and stable on desktop.

An empty work experience list is allowed. Users without prior work history should
not be blocked from using the app.

When the list is empty, show a localized empty-state hint that encourages the
user to add their most recent role first.

Example direction:

- `Add your most recent role first. You can reorder entries later.`

### Duplicate Entry

Each work experience entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

If the source entry was active, the duplicate should also be active immediately
because it already contains user input.

### Sort By Date

Provide a localized `Sort by date (newest first)` action for the work experience
section.

This action should reorder active entries by start date from newest to oldest.

Sorting rules:

- entries with `Currently working here` checked should be treated as the most
  recent,
- when start dates tie, prefer the later end date,
- when end dates also tie, preserve the previous relative order,
- draft entries with no user input should remain at the bottom or be ignored by
  sorting according to one consistent rule and covered by tests,
- manual drag-and-drop order should remain available before and after sorting.

Sorting and drag-and-drop are both supported. Sorting is a convenience action,
not a replacement for manual reordering.

## Fields Per Entry

Each work experience entry should contain these fields:

### Core Fields

- job title,
- company,
- location,
- employment type,
- start date,
- end date,
- currently working here,
- description,
- achievements,
- technologies,
- company URL.

### Field Notes

`Job title` is the role name shown in the CV.

Examples:

- Frontend Developer,
- Project Manager,
- Data Analyst.

`Company` is the employer or client name.

`Location` is optional and should accept simple values such as city/country or
`Remote`. It should not require a full street address.

`Employment type` should be a selectable value with these options:

- Full-time,
- Part-time,
- Contract,
- Freelance,
- Internship.

`Start date` and `End date` should use month + year, not full day precision.

The UI should use dedicated month and year inputs for each date, not free-form
text. Month should be selected from a dropdown or equivalent control. Year
should be entered or selected as a numeric value.

Examples:

- `03 / 2021`,
- `08 / 2024`.

When adding a new work experience entry, `Employment type` should default to
`Full-time`.

`Currently working here` is a checkbox.

When checked:

- the end date field should be disabled or hidden,
- preview and export should show a localized present/current label instead of an
  end date.

`Description` should be a multi-line free-text field for responsibilities and
general role summary.

The UI should show a live character counter for `Description` and `Achievements`
against their maximum allowed length, for example `145 / 2000`.

`Achievements` should be a separate multi-line field for measurable results,
impact, and notable outcomes.

Examples:

- increased conversion by 18%,
- reduced deployment time from 2 hours to 15 minutes,
- led a team of 4 engineers.

`Technologies` should capture tools, frameworks, languages, or platforms used in
the role.

This can be a simple comma-separated text field in this step.

Examples:

- C#, Avalonia, PostgreSQL,
- React, TypeScript, Figma.

`Company URL` is optional and should accept a valid http or https URL.

## Validation Rules

Add validation for every work experience field using the existing C# validation
infrastructure.

Suggested initial rules:

- job title: required, maximum 120 characters,
- company: required, maximum 160 characters,
- location: optional, maximum 120 characters,
- employment type: required, must be one of the supported options,
- start date: required, valid month and year,
- end date: optional when currently working here is checked; otherwise required,
- currently working here: boolean,
- description: optional, maximum 2000 characters,
- achievements: optional, maximum 2000 characters,
- technologies: optional, maximum 500 characters,
- company URL: optional, valid http or https URL, maximum 240 characters.

Additional validation rules:

- month must be between 1 and 12,
- year must be within a sensible range such as 1950 to 2100,
- if both start date and end date are present, start date must not be after end
  date,
- an empty work experience list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, that entry becomes
  active and all validation rules for that entry apply,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active work experience entry contains
  validation errors.

Every work experience field must be covered by the validation schema. Do not
leave any field outside the schema.

Use stable indexed field keys for repeatable entry validation, for example:

- `workExperience.{entryId}.jobTitle`,
- `workExperience.{entryId}.company`.

The exact key format may vary, but validation must support multiple entries
without collisions and must remain inspectable from code.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for work experience entries in `ReVitae.Core`.

The model should support:

- stable entry identity for UI binding and drag-and-drop reordering,
- all fields listed above,
- conversion to and from dictionary-based validation input where useful,
- duplication from an existing entry,
- deterministic date-based sorting for active entries.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Work Experience` section below the existing main personal information
section.

The UI should:

- use expandable top-level sections with default open state,
- refactor the existing main personal information block into the same expandable
  section pattern,
- show one expandable card/section per work experience entry, also default open,
- show a localized empty-state hint when no entries exist,
- allow adding a new entry,
- allow duplicating an existing entry,
- allow removing an entry,
- allow drag-and-drop reordering of the entire entry section,
- provide a `Sort by date (newest first)` action,
- show live character counters for description and achievements,
- show a validation indicator on collapsed entry headers when that entry has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- keep the layout readable and responsive within the current main window,
- disable or hide end date when currently working here is checked.

The drag-and-drop interaction should reorder the underlying entry list immediately
and update preview/export order accordingly.

Removing an entry should not require a confirmation dialog in this step.

The form and preview layout should remain readable and responsive within the
current main window, including when multiple expanded entries are visible.

## Preview

All four existing CV templates should render work experience when active data
exists.

Each entry should show at minimum:

- job title,
- company,
- location when present,
- employment type,
- date range,
- description when present,
- achievements when present,
- technologies when present,
- company URL when present.

Multi-line text in `Description` and `Achievements` should preserve line breaks in
preview and PDF export.

Draft entries with no user input should be omitted from preview and export.

If no active work experience entries exist, the section should be omitted from
preview or shown as empty according to the template style, without breaking
layout.

Template rendering should remain data-driven. The same work experience data must
work across all current templates. Preview should remain responsive when work
experience content grows.

## PDF Export

Plain PDF export should include active work experience entries using the current
entry order.

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
- employment type option labels,
- present/current date label,
- add/remove/reorder related UI text if needed,
- duplicate entry action label,
- sort by date action label,
- empty-state hint text,
- character counter accessibility text if needed,
- validation badge or error-count text for collapsed entries if needed,
- expand/collapse section UI text or accessibility labels if needed,
- all validation messages,
- preview section labels used by templates.

Every supported language must receive the new required translation keys.

## Unit Tests

Add edge case unit tests for the work experience validation schema and any new
core logic.

The tests should cover normal valid values and important edge cases for every
work experience field.

Tests should cover at least:

- valid complete entries,
- empty work experience list,
- draft entries with no input ignored for validation/export,
- entry becoming active after first field input,
- required field failures,
- maximum length boundaries,
- values exactly at and over maximum length,
- invalid and valid company URLs,
- invalid month and year values,
- end date required when not currently working,
- end date optional when currently working,
- start date after end date rejection,
- employment type validation,
- partially filled active entries,
- multiple active entries validated together,
- indexed field key behavior across multiple entries,
- duplicate entry copying all field values into a new identity,
- date-based sorting for newest-first order,
- currently working entries sorting ahead of ended roles with older dates,
- draft entries behavior during sorting,
- translation key usage in schema messages,
- whitespace-only required values inside active entries.

Tests should run through the existing C# lint/test flow.

## Out of Scope

Do not implement these features yet:

- education,
- skills section,
- languages section,
- certificates,
- projects,
- local persistence / saved CV projects,
- template-based PDF export,
- AI-generated descriptions,
- rich text or markdown editor for description/achievements,
- separate technology tag chips UI,
- cloud sync,
- import from existing CV documents.

## Expected Result

ReVitae should support expandable top-level and entry-level form sections with
default open state, including the existing main personal information section.
Collapsed work experience entries should show a readable header summary and a
validation indicator when invalid.
When the section is empty, the app should show a localized empty-state hint.
The app should support multiple work experience entries with full field coverage,
duplicate entry, sort by date, character counters, drag-and-drop reordering of
entire entries, draft-entry validation behavior, live localized validation,
rendering in all four preview templates, and inclusion in plain PDF export. All
work experience fields should be represented in the schema, and validation rules
should have edge-case unit test coverage.
