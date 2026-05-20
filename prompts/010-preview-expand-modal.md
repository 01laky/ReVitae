# Prompt 010 - Preview Expand Modal

Add an expand/resize affordance to the preview panel that opens a larger
in-window preview modal.

## Goal

The right-column CV preview is useful while editing, but the panel is relatively
small. Add a resize/expand icon in the top-right corner of the preview panel
header. Clicking it should open a new modal using the same in-window modal
pattern as Setup and Templates.

The modal body should show the same live CV preview content as the main preview
panel, rendered larger and with its own scroll area so the user can inspect the
current CV layout more comfortably.

This step is UI/UX only. It must not change validation rules, template rendering
logic, PDF export behavior, or form data.

It also includes a small icon fix in the main header: replace the current
templates button icon, which looks too much like the Windows logo, with a more
appropriate Material icon for CV template selection.

## Templates Header Icon Update

File:

- `src/ReVitae/MainWindow.axaml`

The top-right templates button currently uses `ViewGrid`, which reads like a
Windows logo rather than template/theme selection.

Replace it with a clearer template/design icon.

Preferred direction:

- `Palette` or `PaletteOutline`

Acceptable alternatives if they fit visually in the header:

- `Brush`
- `FormatPaint`
- `FileDocumentMultipleOutline`

Do not use grid/tile icons such as `ViewGrid` or `ViewDashboard` for this
button.

Requirements:

- keep the existing `OpenTemplatesButton` behavior unchanged,
- keep localized tooltip and automation name,
- match size and styling of the adjacent setup (`Cog`) button,
- verify the icon reads clearly as templates/design in both light and dark theme.

## Preview Panel Header

File:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Update the preview column header so it contains:

- preview title on the left (`Preview`, localized),
- a Material icon button on the right for expand/resize.

Suggested icon direction:

- `ArrowExpand`, `FitToScreen`, or another Material expand/fullscreen-style icon.

The button should:

- use the existing `re-vitae-icon-button` styling or an equivalent compact header
  action style,
- have a localized tooltip such as `Expand preview`,
- have a meaningful automation name,
- not modify CV data when clicked.

The preview title and expand button should stay aligned on one row, similar to
modal headers elsewhere in the app.

## Preview Expand Modal Shell

Add a third in-window modal shell, independent from Setup and Templates.

Files:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

The modal must follow the same conventions as the existing modals:

- rendered inside the current Avalonia window,
- semi-transparent backdrop using `re-vitae-modal-overlay`,
- centered modal panel using `re-vitae-modal-panel`,
- approximately 80% of current window width,
- approximately 80% of current window height,
- responsive sizing when the app window is resized,
- top close button,
- bottom close button,
- close via `Escape` key,
- preserve all current CV form data and validation state.

Only one app-level modal may be open at a time. Opening preview expand must
close Setup and Templates if they are open, and opening Setup or Templates must
close preview expand.

Suggested XAML names:

- `PreviewExpandModalOverlay`
- `PreviewExpandModalPanel`
- `PreviewExpandTitleTextBlock`
- `PreviewExpandTopCloseButton`
- `PreviewExpandBottomCloseButton`
- `PreviewExpandContentControl`

## Modal Content

The modal body should contain a scrollable enlarged preview of the currently
selected CV template and current form data.

Requirements:

- reuse the same preview-building logic as the main preview panel,
- show the same template variant currently selected in Templates,
- update live when form data changes while the modal is open,
- keep CV template preview content document-style (not app-shell Material
  styling inside the rendered CV itself),
- use a preview surface/container consistent with the main preview column,
- allow vertical scrolling for long CV content.

Do not duplicate preview rendering in two completely separate code paths if a
shared helper can render preview content for both the inline panel and the modal.

Recommended direction:

- extract a shared preview content builder if needed,
- bind/update both `PreviewContentControl` and
  `PreviewExpandContentControl` from the same update method.

## Behavior

Clicking the preview expand button should:

- open the preview expand modal,
- refresh modal preview content immediately,
- keep the inline preview panel unchanged and still visible behind the modal.

Closing the modal should:

- hide the overlay only,
- leave inline preview and form state untouched.

Changing any form field, template selection, or language while the modal is open
should keep the enlarged preview in sync with the inline preview.

## Internationalization

Add translation keys for all new user-facing chrome text.

At minimum:

- expand preview tooltip / action label,
- preview expand modal title,
- reuse existing `Close` key for close buttons if appropriate.

Add keys to:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

Provide translations for every supported language already present in the project.

Do not hardcode new UI strings in XAML or code-behind.

## Accessibility

Preserve or improve accessibility:

- expand button tooltip localized,
- automation name meaningful,
- modal title readable,
- close actions keyboard accessible,
- Escape closes the topmost open modal with correct priority among all modals.

Suggested Escape priority when multiple could theoretically be open:

1. Templates
2. Setup
3. Preview expand

Only one should be visible at a time, but the close order should remain
consistent with existing modal handling.

## Code Changes

Review and update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Also update modal management helpers:

- `SetPreviewExpandModalVisible(bool isVisible)`
- `UpdateModalSizes()` to include the new modal panel
- `OnWindowKeyDown()` to close preview expand modal
- `ApplyLocalization()` for new strings

Keep business logic out of XAML styles. Reuse existing shared UI classes from
`src/ReVitae/Ui/UiClasses.cs` where practical.

## Out of Scope

Do not implement these in this prompt:

- native OS fullscreen window,
- separate detached preview window,
- zoom in/out controls or scale slider,
- print preview,
- PDF export from the modal,
- editing CV fields inside the modal,
- preview-only theme switcher,
- split-screen compare mode,
- preview persistence/history.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass.

Manual UI checks should include:

- templates button uses a non-Windows-like icon (prefer `Palette`),
- expand icon visible in preview header,
- modal opens and closes correctly,
- Escape closes modal,
- modal resizes with window,
- inline preview and modal preview show the same content,
- changing personal info updates both previews,
- changing work experience updates both previews,
- changing template updates both previews,
- opening Setup/Templates closes preview expand and vice versa,
- light and dark theme both look acceptable.

## Expected Result

The templates button in the main header should use a suitable Material icon such
as `Palette` instead of the Windows-like `ViewGrid`.

The preview panel should have a top-right expand/resize icon. Clicking it opens a
responsive in-window modal with a larger scrollable live CV preview, implemented
consistently with the existing Setup and Templates modals and fully integrated
with localization and current preview rendering.
