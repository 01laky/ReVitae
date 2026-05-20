# Prompt 009 - Material.Avalonia App Restyle

Restyle the entire ReVitae application using **Material.Avalonia** so the app
looks like a cohesive, modern Material Design desktop product instead of default
Fluent toolkit styling.

## Goal

Replace the current `FluentTheme` with Material.Avalonia and update every
application UI surface so forms, modals, buttons, cards, validation states, icons,
and layout spacing follow one consistent Material Design system.

This step is a visual/UI refactor only. It must not break existing behavior,
validation, localization, templates, preview rendering logic, PDF export, or
unit tests.

## Design Direction

ReVitae is a professional CV builder. The Material theme should feel:

- clean,
- trustworthy,
- readable,
- suitable for long form editing,
- good on Windows, macOS, and Linux.

Prefer a restrained professional palette rather than flashy colors.

Suggested starting theme:

- `BaseTheme`: follow system by default (`Default`), with Light and Dark support,
- `PrimaryColor`: a professional blue/teal/indigo tone,
- `SecondaryColor`: a subtle accent used sparingly.

The app chrome should use Material styling. CV template preview content inside
the preview panel remains document-style, because it represents exported CV
layout rather than app shell UI.

## Package Setup

Add these NuGet packages to `src/ReVitae/ReVitae.csproj`:

- `Material.Avalonia` (required),
- `Material.Icons.Avalonia` (required for Material icons),
- `Material.Avalonia.Dialogs` (recommended for modal/dialog styling consistency).

Do not add DataGrid or TreeDataGrid packages unless actually needed.

Remove dependency on `Avalonia.Themes.Fluent` if it is no longer used after the
migration.

Update `App.axaml` to use Material theme instead of Fluent theme.

Example direction:

```xml
<Application ...
             xmlns:themes="clr-namespace:Material.Styles.Themes;assembly=Material.Styles"
             RequestedThemeVariant="Default">
  <Application.Styles>
    <themes:MaterialTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Teal" />
  </Application.Styles>
</Application>
```

Also add shared app-level styles in a dedicated resource file such as
`AppStyles.axaml` or `Themes/ReVitaeMaterialStyles.axaml`.

## Global Styling Rules

Create shared Material-friendly layout and spacing rules for the whole app.

Define and reuse:

- page padding,
- section spacing,
- card padding,
- form field spacing,
- modal padding,
- title/subtitle typography,
- primary/secondary/destructive button styles,
- error text styling,
- subtle divider/separator styling.

Avoid hardcoded one-off colors in app UI code where Material theme resources can
be used instead.

Remove emoji-based UI icons from app chrome and replace them with Material icons.

Examples:

- setup → settings icon,
- templates → palette/view-grid icon,
- expand/collapse → chevron icons,
- drag handle → drag icon,
- validation badge → alert/error icon.

Do not change user-entered CV content styling inside preview templates except for
the preview container/chrome around them.

## Files and Surfaces to Restyle

Every current application UI file and surface must be reviewed and updated.

### Application Shell

Files:

- `src/ReVitae/App.axaml`
- `src/ReVitae/App.axaml.cs`
- new shared theme/style resource file(s)

Changes:

- replace Fluent theme with Material theme,
- register Material icons,
- add global app styles,
- keep system theme following behavior when possible.

### Main Window Shell

File:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Restyle all app chrome in the main window:

#### Header Area

- app title,
- subtitle,
- top-right action buttons for Setup and Templates.

These should become proper Material icon buttons or filled tonal buttons with
tooltips and accessible names preserved.

#### Main Form Column

Restyle the full left column, including:

- outer form container/card,
- scroll area,
- expandable `Main / Personal information` section,
- all personal information labels,
- all `TextBox` fields,
- all validation error text blocks,
- validation summary,
- export button,
- export status text.

All personal information fields must visually match Material form controls.

#### Work Experience Section

File:

- `src/ReVitae/WorkExperience/WorkExperienceSectionView.cs`

This file builds much of the work experience UI in code and currently uses
hardcoded brushes/colors/emojis. Refactor it to use Material-friendly styling.

Restyle:

- top-level Work Experience expandable section,
- empty-state hint,
- add/sort action buttons,
- each work experience entry card,
- entry header summary,
- drag handle,
- duplicate/remove buttons,
- all work experience form fields,
- month/year inputs,
- currently working checkbox,
- description/achievements counters,
- field-level validation messages,
- collapsed-entry validation badge.

Replace hardcoded app UI colors such as `#D0D0D0`, `#EFEFEF`, and `IndianRed`
with theme/resource-based styling where appropriate.

#### Preview Column

Restyle the preview panel chrome:

- preview title,
- preview container/card,
- scroll area,
- surrounding borders/background.

The rendered CV template content inside preview may keep its existing template
colors and layout, because those represent CV design variants rather than app
theme.

#### Setup Modal

Restyle the in-window setup modal:

- overlay/backdrop,
- modal card/panel,
- title,
- close buttons,
- placeholder text,
- language label,
- language selector combobox.

The language selector must remain readable and keep flag + native language name
presentation.

#### Templates Modal

Restyle the templates modal:

- overlay/backdrop,
- modal card/panel,
- title,
- close buttons,
- template selection cards/buttons,
- selected state,
- template names/descriptions,
- placeholder thumbnail blocks.

Selected template state should be visually obvious using Material selection
styling.

### Shared Controls

File:

- `src/ReVitae/Controls/ExpandableSection.axaml`
- `src/ReVitae/Controls/ExpandableSection.axaml.cs`

Restyle the reusable expandable section control used by:

- Main / Personal information,
- Work Experience top-level section,
- each work experience entry card.

Requirements:

- Material-style section header,
- clear expand/collapse affordance,
- optional header actions area,
- default expanded state preserved,
- accessible tooltips for expand/collapse actions.

This control should become the standard expandable section pattern for future CV
sections too.

## Control-Specific Requirements

Apply Material styling consistently to all current controls:

- `Window`
- `Button`
- `TextBox`
- `ComboBox`
- `CheckBox`
- `TextBlock`
- `Border`
- `ScrollViewer`
- `Grid`
- `StackPanel`
- modal overlay containers
- cards/panels used as section wrappers

Form requirements:

- labels aligned and readable,
- placeholders preserved,
- multiline fields visually distinct from single-line fields,
- disabled end-date fields visually clear when `Currently working here` is
  checked,
- validation errors use consistent Material error styling,
- export button disabled state remains obvious when validation fails.

Modal requirements:

- responsive sizing behavior from previous prompts must remain,
- Escape key close behavior must remain,
- modal width/height responsive rules must remain.

## Code-Behind Cleanup

Review code-behind UI construction and remove app-shell hardcoded styling where
possible.

Files to review:

- `src/ReVitae/MainWindow.axaml.cs`
- `src/ReVitae/WorkExperience/WorkExperienceSectionView.cs`
- `src/ReVitae/Controls/ExpandableSection.axaml.cs`

Rules:

- app chrome should prefer XAML styles and Material resources,
- hardcoded preview template colors inside CV template builders may remain,
- do not move business logic into styles,
- do not break localization key usage.

If useful, extract repeated UI styling into small reusable helpers or shared
styles rather than duplicating Material setup across files.

## Internationalization

Do not break i18n.

All existing localized strings, tooltips, labels, validation messages, and button
texts must continue to come from the localization layer.

If new UI chrome text is introduced for accessibility or icon buttons, it must
use translation keys and be added to every supported language.

## Accessibility

Preserve or improve accessibility during restyle:

- button/tooltip text remains localized,
- automation names remain meaningful,
- expand/collapse controls remain keyboard accessible where possible,
- validation errors remain readable and associated with fields visually,
- contrast must remain acceptable in both light and dark theme.

## Out of Scope

Do not implement these in this prompt:

- restyling exported PDF content,
- redesigning the four CV template visual identities inside preview,
- changing validation rules,
- changing work experience behavior,
- adding new CV sections,
- local persistence,
- theme picker UI in Setup modal,
- runtime theme switching UI unless required for basic Light/Dark support,
- custom Material theme editor,
- web version of the app.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- the app must build and run on the current Avalonia version.

Manual UI checks should include:

- main window in light theme,
- main window in dark theme if supported by Material theme,
- personal information section expanded/collapsed,
- work experience empty state,
- adding/duplicating/removing/reordering work experience entries,
- validation errors visible on fields and collapsed entry headers,
- setup modal language selector,
- templates modal selected state,
- preview panel still renders all four templates correctly,
- export button disabled/enabled behavior unchanged.

## Expected Result

The entire ReVitae application shell should use Material.Avalonia styling across
all current UI surfaces: header, forms, expandable sections, work experience
cards, validation UI, setup modal, templates modal, and preview chrome.

The app should look like one cohesive Material Design product instead of a
default Avalonia form demo, while preserving all existing functionality,
localization, preview template rendering, and test coverage.
