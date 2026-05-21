# Prompt 031 - CV Image Export (PNG / JPEG / WebP)

Add **raster image export** for the full CV document so users can download page
images for sharing, portfolios, or tools that expect bitmaps (Canva, Notion,
LinkedIn attachments, etc.).

This prompt builds on prompts **019** (template-aligned QuestPDF export), **022**
(export format modal, validation gate, save dialog, post-export shell actions),
and **023** (profile photo in visual exports).

Prompt **022** intentionally excluded image/raster export. This prompt adds it as
a **first-class export path** with user-controlled **image format** and **delivery
mode** (ZIP archive vs separate files).

## Goal

When the user exports as images, they choose **both**:

1. **Image format** — PNG, JPEG, or WebP.
2. **Delivery mode** — **Single ZIP archive** or **Separate image files**.

Export output must remain **template-aligned** with PDF (same QuestPDF layout
pipeline), support **multi-page CVs**, and follow the same validation-gated export
flow as all other formats.

Generation stays **deterministic and local** — no AI, no cloud conversion, no
screenshot of the Avalonia preview control as the primary renderer.

## Current State (Before This Prompt)

Already implemented:

- Export toolbar button opens format modal after validation (prompt 022),
- `CvExportDocument` + `CvExportDocumentMapper` + QuestPDF templates for all
  **16** template families,
- `CvDocumentExporter` facade with PDF and visual/structured writers,
- `CvExportFormatCatalog` with 15 non-image formats,
- save dialog helpers (`CvExportFilePickerOptions`), filename helper
  (`CvExportFilenameHelper`), post-export **Open file** / **Show in folder**
  (`CvExportShellHelper`),
- profile photo embedding in PDF/HTML/DOCX (prompt 023).

Not present:

- PNG / JPEG / WebP export cards or writers,
- image-specific export options UI (format + delivery),
- ZIP packaging of page images,
- folder-based multi-file image export,
- image export tests and documentation.

Prompt **022** acceptance criterion #13 explicitly required **no** PNG/JPEG export
cards — this prompt **adds** them and updates docs/tests accordingly.

## Product Behavior Summary

### Happy path

1. User fills valid CV data and selects a preview template.
2. User clicks **Export** → validation passes → **Export format modal** opens.
3. User clicks the **Images** export card (new category **Images** or under
   **Web & text** — prefer a dedicated **Images** category with one card).
4. Instead of jumping straight to the save dialog, an **Image export options**
   sub-panel appears **inside the export modal** (or a lightweight second step
   overlay — must not open a native OS dialog yet).
5. User configures:
   - **Image format:** PNG (default), JPEG, WebP,
   - **Delivery:** ZIP archive (default) or Separate files.
6. User confirms (**Export images** / primary action on the sub-panel).
7. Save dialog opens according to delivery mode (see below).
8. App renders one bitmap per PDF page, packages output, writes locally.
9. Localized success message + **Open file** / **Show in folder** (ZIP path or
   exported folder).

### Image export options (required UI)

Show these controls when the Images card is selected (before save dialog):

| Control               | Options                               | Default | Notes                                            |
| --------------------- | ------------------------------------- | ------- | ------------------------------------------------ |
| **Image format**      | PNG, JPEG, WebP                       | PNG     | Radio group or segmented control                 |
| **Delivery**          | ZIP archive, Separate files           | ZIP     | Radio group                                      |
| **JPEG/WebP quality** | 70–100 (slider or preset High/Medium) | 90      | Visible only when JPEG or WebP selected          |
| **Scale**             | 1× (screen), 2× (retina / print)      | 2×      | Applies to rasterization DPI; 2× ≈ 192 DPI on A4 |

Localization: add keys under `export.image.*` for labels, hints, and delivery
descriptions.

**Do not** remember last image options across sessions in v1 (same as format
memory deferral in prompt 022).

### Delivery mode behavior

#### ZIP archive (default)

- Save dialog saves **one file**: `{FirstName}_{LastName}_CV_images.zip`
  (use `CvExportFilenameHelper` extension — add `.zip` suffix helper).
- ZIP contains one image per page at ZIP root (no nested folder required in v1):

```text
Jane_Doe_CV_images.zip
├── page-01.png
├── page-02.png
└── page-03.png
```

- Extension inside ZIP matches selected format (`.png`, `.jpg`, `.webp`).
- Single-page CV → ZIP with one image is valid.

#### Separate files

- Open a **folder picker** (not multi-file save dialog) via Avalonia
  `StorageProvider.OpenFolderPickerAsync`.
- Write images directly into the chosen folder:

```text
/Users/jane/exports/
├── Jane_Doe_CV_page-01.png
├── Jane_Doe_CV_page-02.png
└── Jane_Doe_CV_page-03.png
```

- If a filename collision exists, append `-2`, `-3`, … before extension (same
  policy as other export overwrite behavior — fail safe, never silent overwrite
  unless user confirms in save dialog for single files; for folder export document
  the collision suffix rule in Core).
- Post-export **Show in folder** reveals the destination folder; **Open file**
  opens `page-01` (or first page) with the default image viewer.

### Multi-page rules

- Page count must match QuestPDF PDF page count for the same `CvExportDocument`.
- Page indices are **1-based** in filenames: `page-01`, `page-02`, …
- Long CVs (many pages) must not truncate — export all pages.

### Validation and modal rules

- Same validation gate as prompt 022: invalid form → export modal never opens.
- Cancel on image options sub-panel → return to format grid, no save dialog.
- Cancel on save/folder dialog → silent return, no error toast.
- Escape dismisses image options sub-panel first, then export modal (consistent
  with existing modal stack).

### What must NOT happen

- Do **not** rasterize the Avalonia live preview control as the primary output
  (preview layout ≠ PDF template fidelity). Allowed only as documented fallback if
  QuestPDF→bitmap path is blocked on a platform — not the default design.
- Do **not** export one infinitely tall “scroll capture” PNG of the whole CV.
- Do **not** auto-open images after save (same as PDF policy in prompt 022).

## Supported Image Formats

| Format | Extension | Support | Notes                                                            |
| ------ | --------- | ------- | ---------------------------------------------------------------- |
| PNG    | `.png`    | Full    | Lossless; default                                                |
| JPEG   | `.jpg`    | Full    | Quality slider; no alpha                                         |
| WebP   | `.webp`   | Full    | Quality slider; transcode fallback to PNG if encoder unavailable |

### Explicitly excluded (Out of Scope)

- TIFF, HEIC, HEIF, BMP, GIF animation, SVG-as-raster, PDF-to-PNG via external
  cloud API,
- single combined “poster” image stitching all pages into one bitmap,
- batch export of multiple formats in one action,
- editable PSD/AI layers,
- OCR or AI enhancement of exported images,
- embedded metadata beyond basic filename (EXIF author blocks optional nice-to-have
  — skip in v1),
- password-protected ZIP,
- drag-and-drop export target.

## Architecture

### Rendering pipeline (required direction)

```text
CvExportDocument
  └─ QuestPdfCvExporter.Export(document) → PDF bytes (existing 16 templates)
       └─ ICvPdfPageRasterizer.Rasterize(pdfBytes, options) → IReadOnlyList<PageImage>
            └─ per page: encode PNG | JPEG | WebP
                 └─ ICvImageExportPackager
                      ├─ Zip → single .zip stream/file
                      └─ SeparateFiles → write to folder
```

Implement in **`ReVitae.Core/Export/Images/`** (suggested):

| Type / file                   | Responsibility                                    |
| ----------------------------- | ------------------------------------------------- |
| `CvImageExportFormat`         | `Png`, `Jpeg`, `WebP`                             |
| `CvImageExportDelivery`       | `ZipArchive`, `SeparateFiles`                     |
| `CvImageExportOptions`        | Format, delivery, quality, scale record           |
| `ICvPdfPageRasterizer`        | PDF bytes → raw page bitmaps                      |
| `CvImageEncoder`              | Bitmap → encoded bytes per format                 |
| `CvImageExportPackager`       | Build ZIP or write folder                         |
| `CvImageExportFilenameHelper` | `page-01.png`, `Jane_Doe_CV_page-01.png` patterns |
| `CvImageExporter`             | Orchestrates rasterize + encode + package         |

**Rasterizer choice:** prefer a QuestPDF-native or SkiaSharp PDF rasterization path
that runs headless in Core tests. Document the chosen library in
`docs/export-formats.md`. If introducing `SkiaSharp` or `PDFium`-style dependency,
keep license compatible with the repo and note it in README only if required.

### Integration with existing export facade

Extend `CvExportFormat` enum with **`Images`** (one catalog card — format and
delivery are options, not separate enum values).

Alternative rejected: three enum entries (`Png`, `Jpeg`, `WebP`) — would clutter
the modal; user chose combined **Images** card + options panel.

Update:

- `CvExportFormatCatalog` — add Images descriptor + icon asset
  (`export-format-images.svg` under `Assets/ExportFormats/`),
- `CvDocumentExporter.Export(...)` — delegate `CvExportFormat.Images` to
  `CvImageExporter` (requires `CvImageExportOptions` parameter — extend facade
  signature or pass via context object `CvExportRequest`),
- `MainWindow` — collect options from UI, pass to exporter.

### UI layer (`src/ReVitae/`)

| Piece                           | Notes                                                 |
| ------------------------------- | ----------------------------------------------------- |
| `ExportImageOptionsPanel.axaml` | Format + delivery + quality + scale controls          |
| `MainWindow.axaml`              | Host panel inside export modal when Images selected   |
| `CvExportFilePickerOptions`     | ZIP save options vs folder picker for separate files  |
| Localization keys               | All new strings in `TranslationKeys` + `AppLocalizer` |

Flow change in `OnExportFormatSelectedAsync`:

- For non-image formats → existing save dialog path unchanged.
- For `CvExportFormat.Images` → show options panel; on confirm → save/folder
  dialog → export.

## Filename and save dialog rules

| Delivery       | Dialog type | Default name / target    | Filter  |
| -------------- | ----------- | ------------------------ | ------- |
| ZIP            | Save file   | `Jane_Doe_CV_images.zip` | `*.zip` |
| Separate files | Open folder | (user picks folder)      | n/a     |

Reuse `CvExportFilenameHelper.SuggestFilename(first, last, format)` — extend for
`.zip` when delivery is ZIP.

## Profile photo and templates

Exported page images must match PDF export for the selected template:

- sidebar initials fallback when no photo (prompt 023),
- all **16** template families supported (same switch as `QuestPdfCvExporter`),
- Unicode / diacritics render correctly in raster output.

## Tests

Add under `tests/ReVitae.Tests/Export/Images/`:

| Area                          | Minimum tests |
| ----------------------------- | ------------- |
| Filename helper               | 8+            |
| Image encoder (PNG/JPEG/WebP) | 12+           |
| ZIP packager                  | 10+           |
| Separate files packager       | 8+            |
| End-to-end image exporter     | 10+           |
| Collision-safe filenames      | 6+            |

Use minimal PDF fixtures (reuse `MinimalPdfWriter` or QuestPDF test document) for
rasterizer smoke tests. Assert:

- page count matches PDF,
- ZIP contains expected entries,
- separate files land in temp folder with correct extensions,
- JPEG quality changes output size (sanity, not pixel diff),
- invalid/empty document returns `CvExportResult` failure without throw.

Full `./scripts/test.sh` and `npm run lint` must pass.

## Documentation

Update:

- `docs/export-formats.md` — new **Images** section (formats, delivery, limits),
- `README.md` — mention image export in highlights; bump prompts range to `001–031`,
- `CHANGELOG.md` — Unreleased entry,
- `docs/concept.md` — remove “image export future work” if present; note ZIP vs
  separate files,
- cross-link from `docs/import-formats.md` only if symmetric note helps (optional).

## Acceptance Criteria

The prompt is complete when all of the following are true:

1. Export modal includes an **Images** card with official icon and localized label.
2. Selecting Images opens an **options panel** with **image format** (PNG/JPEG/WebP)
   and **delivery** (ZIP / separate files) before any save dialog.
3. **ZIP** delivery produces a `.zip` with `page-NN.{ext}` entries for every PDF
   page.
4. **Separate files** delivery uses a **folder picker** and writes
   `{Name}_CV_page-NN.{ext}` files without silent overwrite on collision.
5. Raster output is **template-aligned** with QuestPDF PDF export for at least
   Classic Sidebar and one extended template (automated smoke + manual QA on all
   16 best effort).
6. Validation gate unchanged — invalid export blocked before modal.
7. Post-export **Open file** / **Show in folder** work for ZIP and folder modes.
8. `CvDocumentExporter` / Core facades own logic — not scattered in `MainWindow`.
9. New image export tests meet minimum counts above; full test suite passes.
10. Docs and CHANGELOG updated; prompt 022 “no PNG/JPEG cards” criterion superseded
    by this prompt in README/release notes.

## Manual QA Checklist

1. Valid CV → Export → Images → defaults (PNG + ZIP) → save ZIP → unzip →
   `page-01.png` opens with correct layout and diacritics.
2. Switch to JPEG + quality 80 → smaller file than PNG at same scale.
3. Switch to WebP → valid file in viewer.
4. Separate files → folder picker → two pages → two files in folder.
5. Single-page CV → ZIP with one image.
6. Multi-page CV (long work history) → page count matches PDF page count.
7. Template switch → image export reflects new template.
8. With profile photo → photo visible in exported image; without photo → initials
   in sidebar templates.
9. Invalid required field → Export blocked; Images path unreachable.
10. Cancel options panel → back to format grid; cancel save dialog → no error.
11. Dark theme → options panel readable.
12. Escape → dismisses options panel, then modal.

## Suggested Implementation Order

1. Core types: `CvImageExportOptions`, format/delivery enums, filename helper.
2. PDF page rasterizer + image encoder (PNG first).
3. ZIP packager + separate-files writer.
4. `CvImageExporter` orchestration + facade wiring.
5. Export modal Images card + SVG icon + localization.
6. `ExportImageOptionsPanel` UI + MainWindow flow (options → save/folder).
7. JPEG/WebP encoders + quality binding.
8. Post-export shell actions for ZIP and folder targets.
9. Tests + documentation pass.

## Expected Result

ReVitae users can export their CV as **PNG, JPEG, or WebP** page images, choosing
at export time whether to download a **single ZIP** or **separate files** in a
folder. Output matches template-aligned PDF layout, supports multi-page CVs, and
fits the existing validation-gated export modal — without AI or cloud conversion.
