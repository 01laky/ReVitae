# Prompt 023 - Profile Picture Upload and Template Integration

Add optional profile picture upload to the `Main / Personal information` section,
wire the stored photo through live preview and all template-aware visual exports,
and support **re-upload by clicking the already uploaded photo** in the form.

Photo remains **optional**. Templates and exports must continue to render cleanly
when no photo is present.

## Goal

Let users add a professional headshot to their CV, see it immediately in the
selected template preview (including the expanded preview modal from prompt 010),
and include it in template-aligned visual exports (PDF, HTML, DOCX; ODT best
effort). Sidebar templates show an **initials avatar** when no photo is set.
JPEG uploads are **EXIF-orientation normalized** on save so mobile photos appear
upright everywhere.

This step should build on:

- the existing personal information form from prompts 002–003,
- the four Avalonia template preview builders in `MainWindow.axaml.cs`
  (`BuildTemplatePreview()` — shared by inline preview and expand modal),
- QuestPDF template export from prompt 019,
- multi-format visual export from prompt 022,
- Material-styled form UI, localization, and inline validation from prompts
  007–018,
- ReVitae JSON / YAML structured export/import from prompts 021–022,
- `Create new CV` reset flow added after prompt 022.

## Current State (Before This Prompt)

Already present:

- `CvExportDocument` exposes `string? PhotoPath`, but `MainWindow.BuildExportDocument()`
  always passes `PhotoPath: null`,
- prompt 006 defined four templates with **optional photo slots**, yet preview and
  PDF builders currently render **no photo area at all**,
- `PersonalInformationImport` and `MainPersonalInformationSchema` contain text
  fields only — no photo field,
- `CvStructuredExportWriter.BuildRevitaeDto(...)` hardcodes `revitaeVersion = 1` and
  serializes `source.Personal` directly into `personalInformation`,
- `ReVitaeJsonMapper` accepts **only** `RevitaeVersion == 1` (strict equality),
- file picker usage exists for CV import/export via Avalonia `StorageProvider`,
- prompt 022 explicitly deferred embedded photo binary export; prompt 019 stated
  “no photo rendering required” for PDF v1.

Not present:

- upload UI in the personal information section,
- local photo storage/copy strategy,
- click-to-reupload interaction,
- photo rendering in preview, PDF, HTML, DOCX,
- ReVitae JSON / YAML round-trip for profile photo,
- tests for photo upload/replace/remove behavior.

## Product Behavior Summary

### First upload

1. User opens `Main / Personal information`.
2. At the top of the section, user sees a profile photo control in **empty
   state** (placeholder avatar + localized upload hint).
3. User clicks the control (or an adjacent explicit upload button if needed for
   accessibility).
4. Native file picker opens, filtered to common image formats.
5. User selects a valid image.
6. App copies the file into app-local storage, updates form state, refreshes
   preview, and shows the photo in the form control.

### Re-upload after photo exists

When a photo is already present:

- the form shows the uploaded image inside the same profile photo control,
- **clicking the displayed photo** opens the file picker again,
- choosing a new image replaces the previous stored copy and updates preview/export
  data immediately,
- the old copied file is deleted from app-local storage when replaced or removed.

This re-upload-on-click behavior is **required** — do not force users to remove
the photo first.

### Remove photo

Provide a localized **Remove photo** action (icon button or text button near the
preview).

Removing a photo should:

- clear form state,
- delete the copied local file,
- refresh preview so template photo areas disappear,
- not affect any text fields.

### Create new CV / clear form / CV import

`StartNewCv()` and `ClearPersonalInformationForm()` must clear profile photo
state and delete any copied photo file for the current session/document.

When the user **imports or replaces a CV** via Upload CV / intro import /
replace flow:

- delete any previously copied profile photo file from app-local storage,
- clear photo UI state before applying imported data,
- restore photo only when the import format supplies one (ReVitae JSON / YAML v2
  with embedded base64),
- document/text imports leave the photo area empty.

`HasCvFormData()` and `HasPersonalInformationData()` must treat an uploaded photo
as personal-section data (so the create-new confirm modal still appears when only a
photo exists).

## UI Requirements

### Placement

Add the profile photo control at the **top of the personal information section**,
before `First name`.

Keep the existing field order for all text inputs unchanged.

### Empty state

Show:

- neutral placeholder avatar (Material icon or simple vector placeholder — no
  external network dependency),
- localized label such as “Profile photo”,
- localized helper text such as “Click to upload a photo”,
- optional explicit “Upload photo” button if the whole card is not keyboard
  activatable on its own.

Use existing classes:

- `re-vitae-form-field`,
- `re-vitae-app-card` spacing conventions,
- Material icon button styling where appropriate.

### Filled state

Show:

- image preview cropped visually to a circle or rounded square according to design
  (consistent across form + templates),
- pointer cursor / hover affordance indicating click-to-change,
- localized tooltip such as “Click to change photo”,
- remove action.

Suggested preview size in form: about **96–120 px** square/circle.

### Validation feedback

Invalid selection must show a localized inline error under the photo control
(same pattern as other personal fields — no bottom summary dump).

Examples:

- unsupported file type,
- file too large (above **15 MB**),
- unreadable/corrupt image,
- file picker cancelled → no error (silent no-op).

Do not block export solely because photo validation failed unless the stored photo
file is missing/corrupt while form state still claims a photo exists.

Photo upload is **optional** — do **not** add required-field rules to
`MainPersonalInformationSchema` for the photo.

## Supported Image Formats

Accept at minimum:

- JPEG / `.jpg`, `.jpeg`
- PNG / `.png`
- WebP / `.webp`

Reject other extensions/MIME types with a localized error.

Explicitly out of scope for v1 picker acceptance:

- HEIC/HEIF (common on iOS — document as limitation, do not half-support).

Recommended limits (adjust only with tests updated):

- max file size **15 MB** (reject at exactly 15 MB + 1 byte; accept at exactly 15 MB),
- no minimum resolution requirement in v1,
- after copy/decode, downscale for UI preview if either dimension exceeds **1024 px**
  (keep stored export file at full resolution up to the size limit; downscale is
  display-only to limit memory use).

Use Avalonia-supported decoding (`Bitmap`, `Image` control). Do not add heavy
image processing libraries for v1 beyond what EXIF orientation normalization
requires (see below).

If QuestPDF or DOCX writers cannot consume WebP directly, transcode to PNG/JPEG
during `ProfilePhotoStorage` copy (same stored file used everywhere).

### EXIF orientation normalization

Mobile JPEGs often appear rotated because EXIF orientation is ignored by default.

During `ProfilePhotoStorage.TrySaveCopy(...)`:

- read EXIF orientation when present (JPEG only is sufficient for v1),
- apply the correct rotation/flip when writing the copied file so stored pixels
  are **visually upright**,
- persist the normalized image as the single source used by form preview, template
  preview, PDF, HTML, and DOCX,
- if EXIF is missing or unreadable, keep pixels as-is (no failure),
- do not expose a manual rotate UI in this prompt.

Full EXIF metadata stripping is **not** required in v1; removing orientation tag
after normalize is acceptable but optional.

## Storage Strategy

Do **not** keep only the user’s original picker path — source files may move or
become unavailable.

Implement a small storage helper, e.g. `ProfilePhotoStorage` in
`src/ReVitae.Core/Cv/` (Core — usable from tests and import mappers):

- copy (normalize EXIF orientation, optionally transcode WebP → PNG/JPEG) into
  app-local storage,
  e.g. `{LocalApplicationData}/ReVitae/profile-photos/{guid}{ext}`,
- store the copied absolute path in UI/session state,
- expose helpers: `TrySaveCopy(...)`, `TryDelete(...)`, `FileExists(...)`,
- delete superseded file on replace/remove/new CV/import replace,
- tolerate missing file on load with localized warning + cleared state (do not
  crash).

Session-only storage is acceptable for v1 (no full project save system yet).

Orphan files under `profile-photos/` from aborted sessions may remain on disk in
v1; do not build a full GC system unless trivial.

## Data Model Changes

### Personal information field key

Add to `MainPersonalInformationFieldKeys`:

```csharp
public const string ProfilePhotoPath = "profilePhotoPath";
```

Use this key for import-confidence tagging if applicable. It is **not** a typed
text field in `MainPersonalInformationSchema`.

### Runtime form state

Keep the active copied photo path in dedicated UI/session state (private field +
helper methods on `MainWindow`, or a small `ProfilePhotoController`).

Optionally mirror into `PersonalInformationImport.ProfilePhotoPath` when building
export source data — but see export rules below.

Extend `PersonalInformationImport` only if it simplifies import mapping:

```csharp
public string ProfilePhotoPath { get; set; } = string.Empty;
```

Update `HasAnyData()` to include non-empty photo path.

**Never** serialize `ProfilePhotoPath` absolute paths into exported JSON/YAML.

### Export document wiring

Pass the active copied photo path from form state into:

- `BuildExportDocument()` → `CvExportDocument.PhotoPath`,
- `BuildExportSourceData()` personal payload (path for in-memory use only).

### ReVitae JSON / YAML round-trip

Extend structured export/import so photo survives `.revitae.json` and YAML export
(both use `BuildRevitaeDto` today).

Required changes:

1. **`CvStructuredExportWriter.BuildRevitaeDto`**
   - stop assigning `source.Personal` directly,
   - build an explicit `personalInformation` object with text fields only,
   - when a photo file exists, read bytes and add:
     - `profilePhotoBase64` (string)
     - `profilePhotoContentType` (string, e.g. `image/jpeg`)
   - set `revitaeVersion` to **2** when photo bytes are included; otherwise keep
     **1** for backward-compatible exports without photo.

2. **`ReVitaeJsonMapper`**
   - replace strict `RevitaeVersion != 1` check with support for **1 and 2**,
   - on version 2 with `profilePhotoBase64`, decode → `ProfilePhotoStorage` → set
     imported `ProfilePhotoPath`,
   - version 1 files import unchanged (no photo).

3. **`MainWindow.ApplyPersonalInformationImport`**
   - after text fields, apply imported photo path to UI control (if file exists).

Do **not** rely on absolute paths inside exported JSON/YAML.

JSON Resume / Europass / HR-XML export may omit photo in v1 unless an obvious
standard field exists; document the limitation rather than inventing incompatible
payloads.

## Template Preview Integration

Update all four preview builders in `MainWindow.axaml.cs` to consume
`document.PhotoPath` when the file exists.

Follow prompt 006 placement rules. When no photo is uploaded, sidebar-style
templates should show an **initials avatar fallback** instead of an empty photo
frame (see below).

| Template            | Photo placement                                                              | No-photo behavior                                           |
| ------------------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------- |
| Classic Sidebar     | top of left sidebar, above name                                              | initials circle (e.g. `LK`) in photo slot; balanced sidebar |
| Modern Sidebar      | top of **left** sidebar column (name stays in dark header band on the right) | initials circle in photo slot; no hollow placeholder        |
| Clean Top Header    | optional small photo in blue header (when present)                           | text-only header when absent (no initials slot)             |
| Dark Sidebar Accent | circular photo near top of dark sidebar, above contact heading               | initials circle styled for dark sidebar; no empty ring      |

### Initials avatar fallback

When `PhotoPath` is empty/missing and the template normally reserves a photo area
(Classic Sidebar, Modern Sidebar, Dark Sidebar Accent):

- derive initials from `FirstName` + `LastName` (first grapheme/letter of each,
  uppercase; if only one name exists use up to two letters from it; if both empty
  use neutral placeholder icon only),
- render a circle with subtle background using existing template accent colors,
- center initials in a readable sans-serif weight,
- reuse one shared helper e.g. `CreateProfilePhotoOrInitialsPreview(...)` for
  Avalonia preview and parallel logic in `CvPdfPhotoHelpers` for PDF,
- HTML/DOCX exports omit initials avatar in v1 unless trivial — photo slot simply
  absent when no photo (document exports stay simpler).

Clean Top Header keeps its current no-photo layout unchanged.

Implementation notes:

- extract shared helpers such as `CreateProfilePhotoOrInitialsPreview(...)` to
  avoid four diverging Avalonia implementations,
- use `Image` + `Bitmap` from local path when photo exists,
- clip to circle via `Border` corner radius or equivalent,
- if file missing but path was set, treat as no photo (initials fallback in
  sidebar templates),

Preview must refresh live after upload, replace, or remove.

Because inline preview and expand modal both call `BuildTemplatePreview()`, one
implementation covers both surfaces.

## PDF Export Integration

Update QuestPDF template renderers:

- `ClassicSidebarPdfTemplate`
- `ModernSidebarPdfTemplate`
- `CleanTopHeaderPdfTemplate` (when preview shows header photo)
- `DarkSidebarAccentPdfTemplate`

When `document.PhotoPath` points to an existing file:

- render image in the same relative placement as preview,
- target reasonable print size (e.g. 80–100 pt square/circle),
- preserve aspect ratio; center crop acceptable.

When no photo:

- sidebar PDF templates render initials fallback matching preview,
- Clean Top Header keeps current text-only header layout.

Add a shared helper e.g. `CvPdfPhotoHelpers.TryComposePhoto(...)` to centralize
existence checks, format compatibility, and sizing.

Ensure export still succeeds if photo file disappeared or format is unsupported
(skip photo, no crash).

## Other Visual Export Integration

Current `CvVisualExportWriter.WriteDocx` uses a **linear** section layout (not
sidebar HTML). Photo in DOCX v1 goes **near the top under name/title**, not in a
fake sidebar column.

| Format   | Photo support v1                                                                 |
| -------- | -------------------------------------------------------------------------------- |
| HTML     | embed `<img>` via base64 data URI in a self-contained file                       |
| DOCX     | embed image near top under name/title (linear writer structure)                  |
| ODT      | best effort: embed if straightforward; otherwise omit with documented limitation |
| RTF      | omit photo in v1 (document limitation)                                           |
| Markdown | omit photo in v1                                                                 |
| TXT      | omit photo in v1                                                                 |
| LaTeX    | omit photo in v1                                                                 |

HTML and DOCX are required for this prompt.

## Import Limitations (Explicit)

Do **not** extract photos from PDF/DOCX/HTML CV imports in this prompt.

Imported CVs may populate text sections only; photo area stays empty until user
uploads manually (except ReVitae JSON/YAML v2 with embedded base64).

Document this in `docs/import-formats.md`.

## Localization

Add keys to:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

Required strings in **all 12 supported languages** (`AppLocalizer.SupportedLanguages`):

- field label: profile photo,
- empty-state hint: click to upload,
- filled-state tooltip: click to change photo,
- upload button text (if separate control),
- remove photo,
- file picker title,
- unsupported file type,
- file too large (mention **15 MB** limit),
- unreadable image,
- photo missing on disk warning (optional toast/status).

Do not hardcode new UI strings in XAML or code-behind.

## Accessibility

- photo control needs `AutomationProperties.Name` localized,
- remove action needs localized name/tooltip,
- keyboard users must be able to trigger upload without mouse-only hover,
- error text associated visually with photo control,
- do not encode essential instructions only in tooltip.

## File Picker Helper

Add `ProfilePhotoFilePickerOptions` in `src/ReVitae/ProfilePhoto/` (or next to
existing import/export picker helpers):

- localized picker title,
- filters for JPEG/PNG/WebP,
- allow single file selection.

Reuse `TopLevel.StorageProvider.OpenFilePickerAsync` pattern from
`MainWindow` import/export handlers.

## MainWindow Integration Checklist

Update `MainWindow.axaml` / `MainWindow.axaml.cs`:

1. add photo control markup at top of personal section,
2. wire click → open picker → `ProfilePhotoStorage` copy → state update,
3. wire remove action,
4. include photo path in `BuildExportDocument()`,
5. clear photo in `ClearPersonalInformationForm()` / `StartNewCv()`,
6. clear photo before `ApplyPersonalInformationImport` during CV import/replace,
7. include photo in `HasPersonalInformationData()`,
8. extend `ApplyPersonalInformationImport` for ReVitae JSON/YAML v2 photo path,
9. extend `BuildExportSourceData()` if personal DTO needs photo path for export
   pipeline (path stays out of serialized JSON fields),
10. call `UpdatePreview()` after photo changes,
11. do not require photo for export validation.

Extract non-trivial logic out of `MainWindow` when it keeps the class readable
(e.g. `ProfilePhotoController` or static helper), but avoid over-abstraction.

## Unit Tests

Add and extend comprehensive unit tests. Follow the edge-case depth used in
prompts 011, 018, and 021 — normal paths alone are not enough.

Test files:

- `tests/ReVitae.Tests/ProfilePhotoStorageTests.cs` (new)
- `tests/ReVitae.Tests/ProfilePhotoInitialsTests.cs` (new — initials derivation/rendering inputs)
- `tests/ReVitae.Tests/Export/CvDocumentExporterEdgeCaseTests.cs` (extend)
- `tests/ReVitae.Tests/Export/CvStructuredExportWriterTests.cs` (extend or create)
- `tests/ReVitae.Tests/Import/Structured/ReVitaeJsonMapperEdgeCaseTests.cs` (extend)
- extend PDF/HTML smoke coverage where helpers are extracted to Core

Do **not** test private `MainWindow.BuildExportDocument()` directly — test through
`CvDocumentExporter` / `CvStructuredExportWriter` / `ProfilePhotoStorage` /
shared photo/initials helpers in Core.

### ProfilePhotoStorage edge cases

- copy creates file in target directory with expected extension,
- replace deletes previous copied file,
- remove/delete helper removes copied file,
- unsupported extension rejected (`.gif`, `.bmp`, `.heic`, `.pdf`),
- file at **15 MB** accepted; **15 MB + 1 byte** rejected,
- empty file rejected,
- corrupt/unreadable bytes rejected,
- EXIF orientation values **1, 3, 6, 8** (minimum set) produce upright stored output,
- JPEG without EXIF still saves successfully,
- WebP transcodes when required by downstream writer compatibility,
- stale path returns false from `FileExists` without throw.

### Export / structured round-trip edge cases

- `CvExportDocument` with photo path → PDF/HTML/DOCX export does not throw,
- export succeeds when photo path set but file missing (skip photo; sidebar PDF uses
  initials if names present),
- `BuildRevitaeDto` emits `revitaeVersion: 2` + base64 when photo present,
- `BuildRevitaeDto` emits `revitaeVersion: 1` when no photo,
- `BuildRevitaeDto` never emits absolute `profilePhotoPath`,
- invalid base64 in JSON v2 import fails gracefully (no crash; photo skipped with
  import still succeeding for text fields if otherwise valid),
- ReVitae JSON v2 round-trip restores decodable photo bytes,
- ReVitae JSON v1 import unchanged (no photo fields),
- YAML export/import inherits JSON photo behavior,
- oversized base64 payload rejected or handled without crash (document chosen behavior
  in tests).

### Initials fallback edge cases

- `John` + `Doe` → `JD`,
- single name `Madonna` → `MA` or `M` (pick one rule, test consistently),
- empty first/last → placeholder path (no throw),
- non-Latin names use first available character grapheme safely,
- initials helper used by preview and PDF paths stays consistent.

### Personal data / import edge cases

- `PersonalInformationImport.HasAnyData()` true when only photo path populated,
- import/replace clears prior stored photo file (test storage helper invocation via
  mapper/controller unit),
- photo from JSON v2 import written to storage and path returned.

Optional UI tests only if an existing pattern exists; do not block the prompt on
Avalonia UI test harness setup.

All tests must pass via `dotnet test` and the repository pre-commit C#/markdown
lint flow.

## Documentation Updates

Review and update **all** documentation that describes personal information,
preview/templates, export, or import behavior. Do not limit edits to the list
below — if a doc becomes inaccurate, fix it in this prompt.

Required updates at minimum:

- `README.md` — optional profile photo upload, 15 MB limit, supported formats,
- `CHANGELOG.md` — Added entry for profile photo, EXIF normalize, initials fallback,
  ReVitae JSON v2 photo field,
- `docs/concept.md` — Phase 1 form includes optional photo,
- `docs/export-formats.md` — per-format photo/initials rules; ReVitae JSON v2 schema
  note; 15 MB source limit,
- `docs/import-formats.md` — no photo from PDF/DOCX/HTML; ReVitae JSON/YAML v2
  base64 photo; HEIC unsupported,
- `prompts/006-basic-html-templates.md` — cross-link note only if template no-photo
  behavior changed from “hide block” to “initials fallback” (optional one-line
  addendum).

Documentation must stay consistent with implemented behavior and tests.

## Code Reuse Rules

Prefer extending existing patterns:

- `StorageProvider` picker options classes,
- `CvExportDocument.PhotoPath` already exists — wire it up,
- preview/PDF section helpers (`CreateSidebarPanel`, `CvPdfLayoutHelpers`),
- localization + inline error pattern from prompt 018,
- `CvStructuredExportWriter` / `ReVitaeJsonMapper` structured pipelines.

Do not introduce a general media library or gallery feature.

Keep the diff focused on profile photo only.

## Out of Scope

Do not implement in this prompt:

- in-app crop/zoom/rotate editor (manual),
- camera capture,
- drag-and-drop onto photo area,
- AI background removal or enhancement,
- photo extraction from PDF/DOCX/HTML imports,
- HEIC/HEIF picker support,
- cloud photo hosting or CDN URLs,
- Gravatar / social auto-fetch,
- multiple photos or gallery,
- photo in JSON Resume/Europass unless trivial and standards-safe,
- PNG/JPEG/WebP **export formats** of the whole CV (still excluded),
- template-aware DOCX sidebar layout (linear DOCX only in v1),
- initials avatar in HTML/DOCX export when no photo (preview + PDF only in v1),
- persisted multi-CV project files beyond copied local photo path,
- GDPR/consent modal copy for photo upload,
- `.revitae.zip` bundle export instead of base64,
- dedicated EXIF privacy stripping UI (orientation normalize is in scope).

## Acceptance Criteria

1. User can upload JPEG/PNG/WebP up to **15 MB** from personal information section.
2. EXIF-oriented JPEGs display upright after upload in form and exports.
3. Uploaded photo appears immediately in inline and expanded template preview.
4. Sidebar templates show **initials fallback** when no photo is set (Clean Top
   Header unchanged).
5. Clicking an already uploaded photo opens picker and replaces it.
6. Remove photo clears UI, storage, and preview (initials return in sidebar templates).
7. Create new CV and CV import/replace clear prior photo unless import restores one.
8. PDF export for all four templates matches preview photo/initials behavior.
9. HTML and DOCX exports embed/show photo when present.
10. ReVitae JSON v2 and YAML round-trip photo data; v1 JSON still imports.
11. Exported JSON/YAML never contains absolute local photo paths.
12. Documentation updated wherever behavior is described; no stale photo guidance.
13. Edge-case unit tests listed above implemented and passing; markdown/C# lint passes.

## Expected Result

ReVitae supports an optional profile photo in the main personal section with
click-to-upload and click-to-reupload UX. JPEG orientation is normalized from EXIF
on import to storage. Sidebar template previews and PDFs show an initials avatar
when no photo is available. HTML/DOCX/PDF include the photo when uploaded.
ReVitae JSON/YAML v2 round-trips the image. Document imports remain text-only for
photos. Docs and edge-case tests are updated to match.
