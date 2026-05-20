# Prompt 004 - Setup Modal Shell

Add the first application-level control panel and setup modal shell.

## Goal

Create a small `Main panel` area in the top-right corner of the main app window.
For now, this panel should contain a single gear/settings icon button.

Clicking the gear button should open a setup modal inside the current app window.
The modal must not open a separate operating system window.

This prompt only defines the modal shell and interaction behavior. The actual
setup content will be defined in a later prompt.

## Main Panel

Add a top-right main panel to the existing window header.

Initial content:

- one gear/settings icon button,
- accessible label or tooltip such as `Open setup`,
- visually aligned to the top-right corner.

The panel should be structured so more app-level controls can be added later.

Examples of future controls that may be added later:

- setup,
- theme switch,
- model status,
- account or profile menu,
- app diagnostics.

Do not implement those future controls in this prompt.

## Gear Button Behavior

When the user clicks the gear/settings button:

1. A setup modal appears inside the current app window.
2. The rest of the app is visually de-emphasized behind the modal.
3. The modal can be closed.
4. Closing the modal returns the user to the same form state as before.

The modal should not reset the CV form, preview, validation state, or entered
data.

## Modal Requirements

The setup modal should behave like an in-window overlay.

It should:

- be rendered inside the existing Avalonia window,
- not create a new native OS window,
- cover the app with a semi-transparent backdrop,
- center the setup panel inside the current window,
- use approximately 80% of the current window width,
- use approximately 80% of the current window height,
- remain responsive when the app window is resized,
- have a clear close button,
- support closing via the `Escape` key if practical.

The 80% sizing should be relative to the current window size. It should not be a
fixed pixel size.

## Setup Content Placeholder

The modal body should contain only placeholder setup content for now.

Suggested placeholder content:

- title: `Setup`,
- short text: `Setup options will be added in a future step.`,
- close button.

Do not add real setup fields yet.

## Accessibility and UX

The setup modal should be easy to understand and close.

The implementation should:

- give the gear button a clear accessible name,
- make the modal title visible,
- provide a visible close action,
- avoid hiding the current app state permanently,
- avoid opening a second app window,
- avoid changing the entered CV data.

## Out of Scope

Do not implement these features yet:

- AI model setup,
- online provider setup,
- user preferences,
- theme settings,
- persistence of setup state,
- multi-step setup wizard,
- account settings,
- cloud sync,
- telemetry settings,
- advanced animations.

## Expected Result

The app should have a top-right main panel with a gear/settings button. Clicking
the button should open a responsive in-window setup modal shell sized around 80%
of the current app window. The modal should be closable and should not affect the
current CV form state.
