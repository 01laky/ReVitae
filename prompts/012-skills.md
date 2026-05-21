# Prompt 012 - Skills

Add the next repeatable CV section: `Skills`.

## Goal

Extend ReVitae with a structured skills section that supports grouped skill
entries, individual skill chips with proficiency and years of experience,
autocomplete suggestions, bulk import, drag-and-drop reordering (groups and
skills), deduplication, validation, localization, preview rendering, and plain
PDF export.

This step should build on:

- the existing `Main / Personal information` section,
- the repeatable section patterns from `Work Experience` and `Education`,
- `ExpandableSection` and Material-styled form UI from prompts 008–011,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts.

Skills should feel like a sibling feature to Work Experience and Education, not a
one-off form block.

Unlike Work Experience and Education, skills entries do **not** use dates.
Ordering is manual through drag-and-drop only.

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Skills` section,
- each individual skill group card inside the section.

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
conventions already used by Work Experience and Education.

## Section Structure

Skills should be implemented as a repeatable list of **skill group entries**.

Each entry represents one logical group of related skills, such as a technology
stack area, tooling category, or soft-skill cluster.

Examples of valid groups:

- Programming Languages,
- Frameworks & Libraries,
- Databases,
- DevOps & Cloud,
- Soft Skills,
- Design Tools.

The user should be able to:

- add a new skill group entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder groups by dragging the entire entry card,
- add individual skills through chip/tag UI,
- remove individual skills from a group,
- drag individual skills between groups,
- bulk-add skills from comma- or newline-separated text,
- edit all fields inside each entry.

There is **no sort-by-date action** in this section. Date-based sorting does not
apply to skills.

Manual drag-and-drop order is the source of truth for preview and export.

An empty skills list is allowed. Users without a dedicated skills section should
not be blocked from using the app.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Group related skills together. You can reorder groups later.`

### Duplicate Entry

Each skill group entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values and nested skill items from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

## Fields Per Group Entry

Each skill group entry should contain these fields:

### Group Fields

- category.

### Individual Skill Items

Each skill inside a group should contain:

- skill name,
- proficiency level,
- years of experience (optional).

### Field Notes

`Category` is the group label shown in the CV.

Examples:

- Programming Languages,
- Frameworks,
- Tools,
- Soft Skills,
- Languages & Frameworks.

`Skill name` is a single skill label.

Examples:

- C#,
- TypeScript,
- Git,
- Communication.

`Proficiency level` should be a selectable value with these options:

- Beginner,
- Intermediate,
- Advanced,
- Expert.

When adding a new skill, `Proficiency` should default to `Intermediate`.

`Years of experience` is optional and should accept whole numbers from 0 to 60.

### Chip / Tag UI

Each added skill should render as a chip/tag inside its group.

Each chip should show at minimum:

- skill name,
- localized proficiency label,
- years of experience when present,
- remove action,
- drag handle for moving the skill between groups or changing order.

The add-skill row should include:

- autocomplete input for skill name,
- proficiency selector,
- optional years input,
- add button.

### Bulk Add

Provide a multi-line bulk input for adding many skills at once.

The user should be able to paste comma-separated values or one skill per line.

Bulk-added skills should use the currently selected proficiency and years values
from the add-skill row.

The bulk input should show a live character counter against its maximum allowed
length, for example `42 / 1000`.

### Autocomplete Suggestions

Provide autocomplete suggestions for common skill names from a static in-app list.

Suggestions should filter as the user types.

The user must still be able to enter custom skill names not present in the
suggestion list.

Do not add AI-generated suggestions in this prompt.

### Deduplication Rules

Apply deduplication when building preview and PDF export data:

- across skill groups: keep the first occurrence of a skill name using
  case-insensitive comparison,
- against Work Experience technologies: omit skills whose names already appear in
  any active work experience entry's `Technologies` field.

The form may keep duplicate values while editing; deduplication is a
preview/export presentation rule and must be covered by tests.

Do not reuse the per-job `Technologies` field from Work Experience as a
substitute for this section. Work Experience technologies remain job-specific;
Skills is a dedicated CV-wide section.

## Validation Rules

Add validation for every skills field using the existing C# validation
infrastructure.

Suggested initial rules:

- category: required for active groups, maximum 120 characters,
- skill name: required for each active skill item, maximum 80 characters,
- proficiency: required for each active skill item, must be one of the supported
  options,
- years of experience: optional, must be between 0 and 60 when provided,
- bulk skills text: maximum 1000 characters in the UI.

Additional validation rules:

- an empty skills list is valid,
- a newly added group with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in category or adds any skill item, that group becomes
  active and all validation rules for that group apply,
- active groups must contain at least one skill item,
- duplicate skill names within the same group must fail validation,
- partially filled active groups should show validation feedback in the UI,
- export should be blocked when any active skill group contains validation
  errors,
- whitespace-only required values inside active groups must fail validation.

Every skills field must be covered by the validation schema. Do not leave any
field outside the schema.

Use stable indexed field keys for repeatable validation, for example:

- `skills.{groupId}.category`,
- `skills.{groupId}.skills`,
- `skills.{groupId}.{skillId}.name`,
- `skills.{groupId}.{skillId}.proficiency`,
- `skills.{groupId}.{skillId}.yearsOfExperience`.

Validation messages must use translation keys and be localized through the
existing i18n layer.

## Data Model

Create a clear typed model for skill groups and skill items in `ReVitae.Core`.

Suggested files:

- `src/ReVitae.Core/Cv/Skills/SkillItem.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsGroupEntry.cs`
- `src/ReVitae.Core/Cv/Skills/ProficiencyLevel.cs`
- `src/ReVitae.Core/Cv/Skills/ProficiencyLevelExtensions.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsFieldKeys.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsSchema.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsTextParser.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsSuggestions.cs`
- `src/ReVitae.Core/Cv/Skills/SkillsDeduplication.cs`

The model should support:

- stable group and skill identity for UI binding and drag-and-drop reordering,
- all fields listed above,
- conversion to dictionary-based validation input where useful,
- duplication from an existing group entry,
- parsing bulk skills text into ordered skill names,
- autocomplete filtering from a static suggestion list,
- preview/export deduplication across groups and against work experience
  technologies,
- header summary generation for collapsed cards,
- draft vs active group detection using the same product rules as Work
  Experience and Education.

Keep CV data separate from template rendering concerns.

## UI Behavior

Add a new `Skills` section below the existing `Education` section.

Suggested UI files:

- `src/ReVitae/Skills/SkillsSectionView.cs`

The UI should:

- use the existing `ExpandableSection` control,
- follow the same Material-friendly styling conventions as Work Experience and
  Education,
- reuse shared UI classes from `src/ReVitae/Ui/UiClasses.cs`,
- reuse `MaterialIconFactory` for drag and validation icons,
- show one expandable card per skill group entry, default open,
- show a localized empty-state hint when no entries exist,
- allow adding, duplicating, removing, and drag-and-drop reordering groups,
- render skill chips/tags with remove buttons and drag handles,
- allow dragging individual skills between groups,
- provide autocomplete on the skill-name input,
- provide bulk add from comma/newline text,
- show a live character counter for bulk skills text,
- show a validation indicator on collapsed entry headers when that group has
  errors,
- validate fields live as values change,
- show field-level validation messages,
- keep the layout readable and responsive within the current main window.

Implement the section primarily in code-behind UI construction, matching the
Work Experience and Education approach, rather than introducing a separate XAML
form for each field.

Removing a group or skill should not require a confirmation dialog in this step.

## Main Window Integration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Integration requirements:

- render `SkillsSectionView` below `EducationSectionView`,
- wire `EntriesChanged` to preview, validation, and export refresh,
- include skills validation in the combined form validation flow,
- block export when active skill groups contain validation errors,
- pass localized strings through `ApplyLocalization()`,
- apply skills deduplication when building preview and PDF export data.

## Preview

All four existing CV templates should render skills when active data exists.

Add skills rendering to the shared preview data model used by template builders
in `MainWindow.axaml.cs`.

Each active skill group should show at minimum:

- category,
- skill items with localized proficiency labels,
- years of experience when present.

Suggested preview section label:

- `Skills`

Draft groups with no user input should be omitted from preview and export.

If no active skill groups remain after deduplication filtering, the skills
section should be omitted from preview without breaking layout.

Template rendering should remain data-driven. The same skills data must work
across all current templates.

Suggested placement in templates:

- after Education when both sections exist,
- before contact/links sections where that produces a readable CV order.

Exact visual styling inside the CV document may remain template-specific, but all
templates must include the skills content consistently.

## PDF Export

Plain PDF export should include active skill groups using the current group
order and the same deduplicated skill items used by preview.

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
- add/duplicate/remove action labels,
- add-skill and bulk-add labels,
- empty-state hint text,
- drag-to-reorder tooltips for groups and skills,
- expand/collapse entry tooltips,
- validation badge or error-count text for collapsed entries,
- preview section label,
- preview years suffix if needed,
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
- chip remove buttons and add-skill controls remain keyboard reachable where
  practical.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/SkillsTests.cs`

The tests should cover normal valid values and important edge cases for every
skills field and collection rule.

Tests should cover at least:

- valid complete groups,
- empty skills list,
- draft groups with no input ignored for validation/export,
- group becoming active after first field input,
- required field failures,
- at-least-one-skill requirement for active groups,
- duplicate skill names within a group,
- maximum length boundaries,
- invalid years of experience,
- proficiency validation,
- partially filled active groups,
- multiple active groups validated together,
- indexed field key behavior across multiple groups and skill items,
- duplicate group copying all nested skill values into new identities,
- skills text parsing for comma-separated values,
- skills text parsing for newline-separated values,
- trimming and empty-item removal during parsing,
- autocomplete suggestion filtering,
- deduplication across groups,
- deduplication against work experience technologies,
- translation key usage in schema messages,
- whitespace-only required values inside active groups,
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
- preview/PDF integration patterns from Work Experience and Education.

Do not duplicate large amounts of Work Experience or Education UI code if a
small shared helper would reduce maintenance without over-engineering.

Keep the diff focused on Skills only.

## Out of Scope

Do not implement these in this prompt:

- languages section,
- certificates as a separate repeatable section,
- projects section,
- local persistence / saved CV projects,
- template-based PDF export redesign,
- AI-generated skill suggestions,
- rich text or markdown editor for skills text,
- skills radar charts or visual level bars,
- cloud sync,
- import from existing CV documents,
- skills-specific preview modal or new app chrome changes.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new skills tests must pass.

Manual UI checks should include:

- skills section visible below education,
- empty-state hint when no entries exist,
- add/duplicate/remove/reorder skill groups,
- add/remove skill chips,
- drag skills between groups,
- autocomplete suggestions while typing,
- bulk add from comma/newline text,
- validation errors on fields and collapsed group headers,
- bulk skills character counter updates live,
- all four preview templates render skills,
- inline preview and expanded preview modal stay in sync,
- preview/export omit duplicate skills across groups,
- preview/export omit skills already listed in work experience technologies,
- PDF export includes skills in group order,
- export blocked when skills validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support a full `Skills` repeatable CV section with grouped skill
entries, chip/tag UI, proficiency and years metadata, autocomplete, bulk import,
drag-and-drop for groups and individual skills, preview/export deduplication,
draft/active validation behavior, live localized validation, rendering in all
four preview templates, and inclusion in plain PDF export.

The form should remain cohesive with the existing Material-styled app shell, and
all skills fields should be represented in the schema with edge-case unit test
coverage.
