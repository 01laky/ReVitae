# Prompt 016 - Links and Additional Information

Add two new CV form sections below `Projects`:

1. `Links` — repeatable custom link entries,
2. `Additional Information` — one optional free-text block.

## Goal

Complete the Phase 1 structured CV form by adding the remaining content areas from
the product concept that are not yet represented as dedicated form sections.

This step should build on:

- the existing `Main / Personal information` section,
- all repeatable section patterns from prompts 008–015,
- `ExpandableSection` and Material-styled form UI,
- validation infrastructure, template preview system, and internationalization
  layer from previous prompts,
- the corrected drag-and-drop approach used by `LanguagesSectionView`,
  `CertificatesSectionView`, and `ProjectsSectionView`.

These sections must remain **separate from personal information**. Do not remove,
relocate, or migrate the existing LinkedIn, portfolio, or GitHub fields from the
main personal section in this prompt.

## Important Distinction

The app already stores three common profile links inside
`Main / Personal information`:

- LinkedIn URL,
- portfolio / website URL,
- GitHub URL.

The new `Links` section is for **additional custom links** only, such as:

- Behance,
- Dribbble,
- Medium,
- Dev.to,
- ORCID,
- Google Scholar,
- Stack Overflow,
- personal blog mirrors,
- other professional profiles.

Do not duplicate those three built-in personal-info URLs into the new links list
automatically, and do not require users to re-enter them here.

Preview and export should continue to show the existing personal-info URLs in
their current contact/links areas, while the new `Links` section renders only
the additional custom link entries.

## Part 1 - Links

## Expandable Sections

Reuse the existing expandable section interaction model.

This applies to:

- the top-level `Links` section,
- each individual link entry card inside the section.

Requirements:

- default state for every expandable section is **open/expanded**,
- collapsing a section must not hide, clear, or disable validation,
- collapsed entry headers must show a short readable summary,
- collapsed entry headers must show a validation indicator when that entry has
  errors,
- drag-and-drop reordering must still work when entries are expanded or
  collapsed,
- drag handle and expand/collapse controls must not conflict.

## Section Structure

Links should be implemented as a repeatable list of entries.

Each entry represents one additional external link shown on the CV.

The user should be able to:

- add a new link entry,
- duplicate an existing entry,
- remove an existing entry,
- reorder entries by dragging the entire entry card,
- edit all fields inside each entry.

There is **no sort-by-date action** in this section. Manual drag-and-drop order
is the source of truth for preview and export.

An empty links list is allowed.

When the list is empty, show a localized empty-state hint.

Example direction:

- `Add links to profiles, portfolios, or publications that are not already in your main contact details.`

### Duplicate Entry

Each link entry should provide a `Duplicate` action.

Duplicating an entry should:

- create a new entry with a new stable identity,
- copy all field values from the source entry,
- insert the duplicate near the source entry unless manual order is changed
  afterward,
- open the duplicated entry expanded by default,
- treat the duplicate according to the same draft/active validation rules as any
  other entry.

## Fields Per Link Entry

Each link entry should contain these fields:

### Core Fields

- label,
- URL,
- note.

### Field Notes

`Label` is the short name shown beside or above the URL in preview and export.

Examples:

- Behance,
- Medium,
- ORCID,
- Google Scholar,
- Stack Overflow.

The UI may optionally provide autocomplete suggestions for common link labels
from a static in-app list, while still allowing custom labels.

`URL` must be a valid http or https URL.

Examples:

- `https://behance.net/username`,
- `https://orcid.org/0000-0002-1825-0097`.

`Note` is optional and should accept short supporting text.

Examples:

- design portfolio,
- selected publications,
- open-source profile.

This field is for short notes only, not long descriptions.

## Links Validation Rules

Suggested initial rules:

- label: required for active entries, maximum 80 characters,
- URL: required for active entries, valid http or https URL, maximum 240
  characters,
- note: optional, maximum 120 characters.

Additional validation rules:

- an empty links list is valid,
- a newly added entry with no user input in any field should be treated as a
  draft and should not block export,
- once the user enters data in any field inside an entry, that entry becomes
  active and all validation rules for that entry apply,
- duplicate URLs across active link entries must fail validation using
  case-insensitive comparison,
- partially filled active entries should show validation feedback in the UI,
- export should be blocked when any active link entry contains validation
  errors,
- whitespace-only required values inside active entries must fail validation.

Use stable indexed field keys, for example:

- `links.{entryId}.label`,
- `links.{entryId}.url`,
- `links.{entryId}.note`.

## Links Data Model

Suggested files:

- `src/ReVitae.Core/Cv/Links/LinkEntry.cs`
- `src/ReVitae.Core/Cv/Links/LinksFieldKeys.cs`
- `src/ReVitae.Core/Cv/Links/LinksSchema.cs`
- `src/ReVitae.Core/Cv/Links/LinksCollectionValidator.cs`
- `src/ReVitae.Core/Cv/Links/LinkPreviewFormatter.cs`
- `src/ReVitae.Core/Cv/Links/LinkLabelSuggestions.cs`

The model should support:

- stable entry identity,
- all link fields listed above,
- conversion to dictionary-based validation input,
- duplication,
- header summary generation for collapsed cards,
- draft vs active entry detection using the same product rules as other
  repeatable sections.

Suggested draft/active behavior:

- default empty entry is a draft,
- any non-whitespace value in any field makes the entry active.

## Links UI

Suggested UI file:

- `src/ReVitae/Links/LinksSectionView.cs`

Place `LinksSectionView` below `ProjectsSectionView`.

The UI should:

- use `ExpandableSection`,
- follow existing Material-friendly styling conventions,
- reuse `UiClasses` and `MaterialIconFactory`,
- show one expandable card per link entry, default open,
- show a localized empty-state hint when no entries exist,
- allow add, duplicate, remove, and drag-and-drop reordering,
- optionally provide autocomplete on the label input,
- validate fields live and show field-level validation messages.

Implement drag-and-drop using the same pointer-capture pattern as
`ProjectsSectionView`.

## Part 2 - Additional Information

## Additional Information Structure

`Additional Information` should be a **single optional free-text section**, not
a repeatable list.

It is for content that does not fit cleanly into the other structured sections,
such as:

- hobbies and interests,
- volunteering,
- publications not tied to a project,
- awards or honors,
- conference talks,
- visa/work authorization notes when appropriate,
- other custom CV notes.

The section should:

- use one top-level `ExpandableSection`,
- default to open/expanded,
- contain one multi-line text input,
- show a live character counter against the maximum allowed length,
- remain fully optional — empty content must not block export.

Example empty-state hint direction:

- `Use this section for interests, volunteering, publications, or other notes that do not belong elsewhere.`

There is no draft/active entry model here. Validation applies only when the user
enters text.

## Additional Information Field

Suggested field:

- content.

Suggested validation:

- optional,
- maximum 3000 characters.

The UI should show a counter such as `145 / 3000`.

## Additional Information Data Model

Suggested files:

- `src/ReVitae.Core/Cv/AdditionalInformation/AdditionalInformationContent.cs`
- `src/ReVitae.Core/Cv/AdditionalInformation/AdditionalInformationFieldKeys.cs`
- `src/ReVitae.Core/Cv/AdditionalInformation/AdditionalInformationSchema.cs`
- `src/ReVitae.Core/Cv/AdditionalInformation/AdditionalInformationValidator.cs`

Suggested field key:

- `additionalInformation.content`

The model may be a simple typed object with one `Content` string property and a
helper such as `HasUserInput()`.

## Additional Information UI

Suggested UI file:

- `src/ReVitae/AdditionalInformation/AdditionalInformationSectionView.cs`

Place `AdditionalInformationSectionView` below `LinksSectionView`.

The UI should:

- use `ExpandableSection`,
- contain one multiline text box,
- show a live character counter,
- validate on change,
- show field-level validation feedback when over the maximum length.

## Main Window Integration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Integration requirements:

- render `LinksSectionView` below `ProjectsSectionView`,
- render `AdditionalInformationSectionView` below `LinksSectionView`,
- wire change events to preview, validation, and export refresh,
- include links and additional-information validation in the combined form
  validation flow,
- block export when active link entries contain validation errors or when
  additional-information content exceeds its maximum length,
- pass localized strings through `ApplyLocalization()`.

Do not remove or hide the existing LinkedIn, portfolio, or GitHub fields from the
main personal section.

## Preview

All four existing CV templates should render the new content when present.

## Links Preview

Add additional-links rendering to the shared preview data model used by template
builders in `MainWindow.axaml.cs`.

Each active link entry should show at minimum:

- label,
- URL,
- note when present.

Suggested preview formats:

- `Behance: https://behance.net/username`,
- `ORCID: https://orcid.org/0000-0002-1825-0097 — research identifier`.

Suggested preview section label:

- `Links`

Draft link entries with no user input should be omitted.

If no active link entries exist, omit the links section from preview without
breaking layout.

Suggested placement:

- after `Projects` when present,
- before the existing personal contact/link blocks only if that produces a more
  readable order; otherwise place after projects and keep personal-info URLs in
  their current contact sections.

Do not remove or relocate existing personal-info URL rendering.

## Additional Information Preview

When `additionalInformation.content` contains non-whitespace text, render a
dedicated preview section in all four templates.

Suggested preview section label:

- `Additional Information`

Render the text as plain paragraphs/lines using the same lightweight text rules
as other multiline CV content.

If the content is empty, omit the section entirely.

Suggested placement:

- near the end of the CV body, typically after links/projects and before or
  alongside contact sections depending on the template, using the most readable
  order consistently across all templates.

## PDF Export

Plain PDF export should include:

- active additional link entries in current entry order,
- additional-information content when present.

Use the same formatting rules as preview.

Existing personal-info URLs must continue to export through the current main
section PDF flow unchanged.

If validation fails, export must remain blocked and show the existing localized
validation feedback pattern.

## Internationalization

Add translation keys for all new user-facing text.

This includes:

- both section titles,
- all field labels and placeholders,
- add/duplicate/remove action labels,
- empty-state hints,
- drag/expand/collapse tooltips,
- validation badge text for collapsed link entries,
- preview section labels,
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
- validation errors remain readable and associated with fields,
- multiline additional-information input remains keyboard accessible.

## Unit Tests

Add comprehensive unit tests in:

- `tests/ReVitae.Tests/LinksTests.cs`
- `tests/ReVitae.Tests/AdditionalInformationTests.cs`

Links tests should cover at least:

- valid complete entries,
- empty links list,
- draft entries ignored for validation/export,
- entry becoming active after first field input,
- required field failures,
- maximum length boundaries,
- invalid and valid URL values,
- duplicate URLs across active entries,
- duplicate entry copying all field values with new identity,
- indexed field key behavior across multiple entries,
- optional label autocomplete filtering when implemented,
- preview formatting with and without notes,
- translation key usage in schema messages,
- whitespace-only required values inside active entries,
- header summary generation for collapsed cards.

Additional-information tests should cover at least:

- empty content is valid,
- content exactly at and over maximum length,
- `HasUserInput()` behavior,
- translation key usage in schema messages,
- validator integration through the schema/helper used by MainWindow validation.

Tests should run through the existing C# lint/test flow.

## Code Reuse Rules

Prefer extending existing patterns over inventing new ones.

Reuse where practical:

- `ExpandableSection`,
- `UiClasses`,
- `MaterialIconFactory`,
- collection validator structure,
- draft/active entry behavior from other repeatable sections,
- drag-and-drop pattern from `ProjectsSectionView`,
- preview/PDF integration patterns from Certificates and Projects,
- URL validation rules already used in personal information and project URL
  fields.

Keep the diff focused on Links and Additional Information only.

## Out of Scope

Do not implement these in this prompt:

- moving LinkedIn, portfolio, or GitHub out of personal information,
- syncing or deduplicating personal-info URLs against the new links section,
- local persistence / saved CV projects,
- rich text or markdown editor for additional information,
- repeatable additional-information entries,
- link favicons, QR codes, or clickable preview rendering beyond plain text,
- template-based PDF export redesign,
- AI-generated link suggestions,
- cloud sync,
- import from existing CV documents,
- new app chrome beyond the two new form sections.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new links and additional-information tests must pass.

Manual UI checks should include:

- links section visible below projects,
- additional-information section visible below links,
- empty-state hints when links list is empty,
- add/duplicate/remove/reorder link entries,
- drag-and-drop reorder works for link entries,
- personal-info LinkedIn/portfolio/GitHub fields still work unchanged,
- additional links appear in preview/PDF separately from personal-info URLs,
- additional-information text appears in preview/PDF when filled,
- validation errors on link fields and collapsed entry headers,
- all four preview templates render both new sections consistently,
- inline preview and expanded preview modal stay in sync,
- export blocked when links validation fails,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should support:

- a repeatable `Links` section for additional custom profile links, and
- a single optional `Additional Information` free-text section,

both integrated with validation, localization, preview across all four templates,
and plain PDF export.

The existing personal-information link fields must remain in place and continue
to work as before.

After this prompt, the Phase 1 structured CV form content model from the product
concept should be complete except for persistence and export enhancements.
