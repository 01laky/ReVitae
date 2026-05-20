# Prompt 006 - Basic HTML CV Templates

Add the first four selectable CV templates.

## Goal

Extend the Templates modal from Prompt 005 so the user can choose one of four
basic CV templates. The selected template should be used in the app preview and
should render the current form data.

Templates should be implemented as HTML/CSS-style layouts or an equivalent
template abstraction that can later be rendered consistently into PDF.

The preferred direction is HTML/CSS templates because:

- CV layouts are naturally document-like,
- the same structure can power preview and future PDF export,
- templates can stay separated from CV data,
- adding new templates later should be mostly a template file/change, not app
  logic.

## Template Data

Templates should consume the current structured CV data from the form.

Current supported data:

- first name,
- last name,
- professional title,
- email,
- phone,
- location,
- LinkedIn URL,
- portfolio or website URL,
- GitHub URL,
- short summary.

Photo should be supported by the template model as optional data, but the app
does not need to implement photo upload yet. If no photo is available, templates
must still render cleanly.

## Initial Templates

Add four starter templates based on these visual directions.

### Template 1 - Classic Sidebar With Photo

A two-column CV layout with a light gray left sidebar.

Visual direction:

- left sidebar for name, optional photo, contact details, and secondary sections,
- main white content area on the right,
- strong section headings,
- clean black/gray typography,
- optional accent color for part of the name.

If no photo exists, the sidebar should not leave an awkward empty image block.

### Template 2 - Modern Sidebar With Header Band

A two-column layout with a left profile area and a dark horizontal name band.

Visual direction:

- optional photo at the top-left,
- left sidebar for contact and highlights,
- large dark name band across the page,
- main content area on the right,
- compact professional style.

If no photo exists, the header/sidebar should adapt without breaking layout.

### Template 3 - Clean Top Header

A single-page clean layout with a colored top header.

Visual direction:

- full-width colored header at the top,
- large name on the left,
- contact information on the right,
- white content area below,
- simple section separators,
- no required photo.

This should be the safest default template because it works well without a photo.

### Template 4 - Dark Sidebar Accent

A two-column layout with a dark left sidebar and a colored header band.

Visual direction:

- dark sidebar for contact and secondary information,
- large colored header area,
- optional circular photo near the top,
- main content area with strong section headings,
- more visual personality than the clean template.

If no photo exists, the circular photo area should be hidden or replaced by a
clean layout state.

## Templates Modal Behavior

The Templates modal should show the four templates as selectable cards.

Each card should include:

- template name,
- short description,
- small visual placeholder or thumbnail-like block,
- selected state when active.

Clicking a template card should:

1. mark the template as selected,
2. close the modal or clearly show the selected state,
3. update the preview to use the selected template,
4. keep all current form data unchanged.

It is acceptable in this step to use simple placeholder thumbnails instead of
real screenshots.

## Preview Behavior

The preview panel should render the selected template using current form data.

When the user edits a field, the selected template preview should update live.

If a field is empty, the template should either omit that line or use the same
safe placeholder behavior already used by the app. Avoid broken or ugly empty
sections.

## PDF Export Behavior

The PDF export should keep working.

If full HTML-to-PDF export is too large for this step, keep the current plain PDF
export behavior and document that template-based PDF export will be added later.

Do not break existing export validation.

## Out of Scope

Do not implement these features yet:

- photo upload,
- real template screenshots,
- custom template import,
- paid templates,
- template categories,
- template search,
- drag-and-drop template editing,
- full multi-section CV data,
- AI template recommendations.

## Expected Result

The app should include four basic selectable CV templates. The Templates modal
should let the user choose one, and the preview should render the current form
data using the selected template. Photo support should be optional in the
template structure, but photo upload is not required yet.
