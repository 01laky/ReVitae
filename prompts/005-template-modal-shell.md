# Prompt 005 - Template Modal Shell

Extend the top-right main panel with a template selection entry point.

## Goal

Improve the existing main panel from Prompt 004 and add a second app-level
control for future CV template selection.

This prompt has two parts:

1. Fix the gear/settings icon alignment so it is visually centered vertically in
   the first row of the panel.
2. Add a palette/template icon button that opens a new in-window modal shell for
   template selection.

The template modal should only be a shell for now. Real templates and selection
logic will be added later.

## Gear Icon Alignment

The existing gear/settings icon should be visually centered inside its button and
aligned vertically with the first row/header area.

The goal is to make the button feel clean and intentional:

- the icon should not look too high or too low,
- the button should not stretch the header unnecessarily,
- the settings button should stay in the top-right main panel,
- the existing setup modal behavior should continue to work.

## Main Panel Layout

The main panel should support multiple app-level icon buttons.

After this prompt, it should contain:

- gear/settings icon button,
- palette/template icon button.

The buttons should be visually aligned with each other and should use consistent
sizing, spacing, and hover/click behavior where practical.

## Palette Icon Button

Add a new icon button to the top-right main panel.

The button should:

- use a palette or template-related icon,
- have an accessible label or tooltip such as `Open templates`,
- be placed next to the gear/settings button,
- not change any CV form data when clicked.

## Template Modal Behavior

Clicking the palette/template button should open a separate template modal shell
inside the current app window.

The modal must:

- be rendered inside the existing Avalonia window,
- not create a new native OS window,
- use a semi-transparent backdrop,
- be centered in the current window,
- use approximately 80% of the current window width,
- use approximately 80% of the current window height,
- remain responsive when the app window is resized,
- have a clear close button,
- support closing via the `Escape` key if practical,
- preserve all current CV form data and validation state.

This should be a separate modal state from the setup modal. Opening templates
should not open setup, and opening setup should not open templates.

## Template Modal Placeholder Content

The modal body should contain placeholder template content only.

Suggested placeholder content:

- title: `Templates`,
- short text: `Template selection will be added in a future step.`,
- close button.

Do not add real templates yet.

## Out of Scope

Do not implement these features yet:

- actual CV templates,
- template previews,
- template selection persistence,
- PDF layout changes,
- HTML template rendering,
- custom template upload,
- template categories,
- template search,
- paid or locked templates.

## Expected Result

The top-right main panel should have a vertically centered gear/settings icon and
a new palette/template icon. The palette button should open a responsive
in-window template modal shell that is independent from the setup modal and does
not affect the current CV form state.
