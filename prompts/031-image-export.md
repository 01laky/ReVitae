# Prompt 031 - CV Image Export (PNG / JPEG / WebP)

Add **raster image export** for the full CV document so users can download page
images for sharing, portfolios, or tools that expect bitmaps (Canva, Notion,
LinkedIn attachments, etc.).

Builds on prompts **019** (template-aligned QuestPDF export), **022** (export
format modal, validation gate, save dialog, post-export shell actions), and **023**
(profile photo in visual exports).

Prompt **022** intentionally excluded image/raster export. This prompt adds it as
a **first-class export path** with user-controlled **image format**, **delivery
mode** (ZIP archive vs separate files), **page range**, and **export progress**
feedback.

Does **not** implement AI enhancement, cloud conversion, or full OCR import parity
(see prompt **032** for image **import** via OCR).

## Goal

When the user exports as images, they choose:

1. **Image format** — PNG, JPEG, or WebP.
2. **Delivery mode** — **Single ZIP archive** or **Separate image files**.
3. **Page range** — all pages (default) or a **From–To** inclusive range.
4. **Scale** and **quality** (JPEG/WebP) with a **size estimate** hint before export.

Export output must remain **template-aligned** with PDF (same QuestPDF layout
pipeline), support **multi-page CVs**, and follow the same validation-gated export
flow as all other formats.

Generation stays **deterministic and local** — no AI, no cloud conversion, no
screenshot of the Avalonia preview control as the primary renderer.

## Non-Goals (This Prompt)

- TIFF, HEIC, BMP, GIF, SVG-as-raster, PDF-as-image via external API,
- single combined “poster” image stitching all pages into one bitmap,
- batch export of multiple image formats in one action,
- editable PSD/AI layers,
- OCR or AI enhancement **during** export,
- password-protected ZIP,
- drag-and-drop export target,
- remembering last image options across sessions (v1),
- auto-opening images after save (same policy as PDF in **022**),
- rasterizing the Avalonia live preview control as the primary output path,
- cancellable mid-export UI (v1: disable duplicate clicks + progress text only).

## Current State (Before This Prompt)

Already implemented:

| Area                                            | Location                                                                                       |
| ----------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Export toolbar → validation → format modal      | `MainWindow.axaml.cs` (`OnExportPdfClicked`, `OpenExportModal`, `OnExportFormatSelectedAsync`) |
| Export format grid host                         | `MainWindow.axaml` → `ExportFormatCategoriesPanel`                                             |
| 15 non-image format cards                       | `CvExportFormatCatalog`, `CvExportFormat` enum                                                 |
| QuestPDF PDF export (16 templates)              | `QuestPdfCvExporter`, `src/ReVitae.Core/Export/Pdf/Templates/`                                 |
| Export facade (single-file `Stream`)            | `CvDocumentExporter.Export(document, source, format, stream)`                                  |
| Filename helper                                 | `CvExportFilenameHelper.SuggestFilename(...)` → `{First}_{Last}_CV{suffix}{ext}`               |
| Save dialog defaults                            | `CvExportFilePickerOptions`, `CvExportSaveDialogDefaults`                                      |
| Post-export actions                             | `CvExportShellHelper`, `_lastExportedFilePath` in `MainWindow`                                 |
| Profile photo in PDF/HTML/DOCX                  | `BuildExportDocument()` → `CvExportDocument.PhotoPath`                                         |
| **SixLabors.ImageSharp** (encode PNG/JPEG/WebP) | `ReVitae.Core.csproj`, `ProfilePhotoStorage.cs`                                                |
| **Docnet.Core** PDF → bitmap (OCR import)       | `Import/Ocr/DocnetPdfPageRenderer.cs` — reuse rendering pattern only                           |
| OCR import pipeline (**032** partial)           | `CvDocumentImporter`, `OcrImageTextExtractor` — for round-trip smoke only                      |

Not present:

- `CvExportFormat.Images` catalog card and **Images** category,
- image options sub-panel in export modal,
- PDF page rasterizer for **export** (separate from OCR import limits/diagnostics),
- ZIP / separate-files packaging,
- page range selection, size estimate, export progress UI,
- image export tests and documentation updates.

Prompt **022** excluded raster formats and “ZIP bundles of **multiple formats**” —
this prompt adds a **single-format** page-image ZIP, which is not a multi-format bundle.

## Product Behavior Summary

### Happy path

1. User fills valid CV data and selects a preview template.
2. User clicks **Export** → validation passes → **Export format modal** opens.
3. User clicks the **Images** card (new category **Images**).
4. Export modal **stays open**; format grid hides; **Image export options** sub-panel
   appears inside the same modal (not a native OS dialog yet).
5. User configures format, delivery, quality, scale, and page range.
6. **Size estimate** updates live under the controls (see **Size estimate**).
7. User confirms (**Export images** primary action).
8. Save dialog (ZIP) or **folder picker** (separate files) opens.
9. App shows **export progress** while rendering (see **Export progress**).
10. App renders selected pages, encodes, packages, writes locally.
11. Localized success message + **Open file** / **Show in folder**.

### Image export options (required UI)

Show when the Images card is selected (before save/folder dialog):

| Control               | Options                               | Default | Notes                                   |
| --------------------- | ------------------------------------- | ------- | --------------------------------------- |
| **Image format**      | PNG, JPEG, WebP                       | PNG     | Radio group or segmented control        |
| **Delivery**          | ZIP archive, Separate files           | ZIP     | Radio group                             |
| **JPEG/WebP quality** | 70–100 (slider or preset High/Medium) | 90      | Visible only when JPEG or WebP selected |
| **Scale**             | 1× (screen), 2× (retina / print)      | 2×      | Maps to Docnet render size (see below)  |
| **Pages**             | All pages, Page range (From–To)       | All     | See **Page range**                      |

Add **Back** control returning to format grid without closing modal.

Top **Close** (×) while options panel is visible: same as **Back** (return to grid),
not instant dismiss of entire export modal — keeps Escape stack predictable.

Localization: keys under `export.image.*` and `export.category.images` (see **Localization**).

### Page range

| Mode          | UI                                                          | Export behavior                                   |
| ------------- | ----------------------------------------------------------- | ------------------------------------------------- |
| **All pages** | Selected by default; hide From/To inputs                    | Export every PDF page (subject to `MaxPageCount`) |
| **Range**     | Two numeric inputs **From** and **To** (1-based, inclusive) | Export only pages in `[From, To]`                 |

Rules:

- **From** ≤ **To**; both ≥ 1 and ≤ total page count.
- Invalid range → inline validation on options panel; **Export images** disabled until fixed.
- Default when switching to Range on a 3-page CV: From=1, To=3.
- Filenames keep **original page indices**: exporting range 2–3 yields `page-02`, `page-03`
  (not renumbered `page-01`, `page-02`).
- ZIP and separate-files modes both respect range (only selected pages included).
- Size estimate recalculates when range changes.

Store range in `CvImageExportOptions`:

```csharp
public sealed record CvImagePageRange(int? FromPage, int? ToPage)
{
    public static CvImagePageRange AllPages => new(null, null);
    public bool IsAllPages => FromPage is null && ToPage is null;
}
```

Core helper `CvImagePageRangeResolver.Resolve(totalPages, range)` → ordered list of
1-based page indices to export.

### Size estimate

Show a secondary hint line below the options (updates when format, quality, scale,
or page range changes):

```text
3 pages · ~2.4 MB (PNG, 2×)
```

Implementation:

- Add `CvImageExportSizeEstimator.Estimate(pageCount, format, scale, quality)` in Core.
- Heuristic is acceptable (not byte-exact): e.g. `pageCount × baseBytesPerPage(format, scale) × qualityFactor`.
- Calibrate constants from representative fixture exports in tests (sanity bounds, not golden bytes).
- When page count unknown until PDF generated, estimate after QuestPDF byte generation or use
  last-known page count cached when options panel opens (refresh on confirm if needed).
- Show `—` or “Calculating…” briefly if page count not yet known; never block export.
- Localized pattern: `export.image.sizeEstimate` → `{0} pages · ~{1} ({2}, {3})`.

### Export progress

While `CvImageExporter` runs (may take several seconds on long CVs):

- Disable **Export images** and format controls to prevent duplicate export.
- Show inline progress in the options panel **or** a thin progress strip in the export modal:
  - `Rendering page 3 of 12…` (`export.image.progressRendering`)
  - then `Writing files…` (`export.image.progressWriting`) during ZIP/folder packaging.
- Run export on a background thread / `Task.Run` so UI stays responsive; marshal progress updates
  to UI thread.
- On failure, re-enable controls and show existing failure keys.
- On success, close modal flow continues as today (success text in main window status area).

Do **not** implement cancel button in v1.

### Opaque white background

Page images must look like **printed CV pages**, not transparent overlays:

- After Docnet rasterize, composite each page onto an **opaque white** (`#FFFFFF`) background
  before encode if the bitmap has alpha or transparent regions.
- **JPEG** output is always opaque (no alpha channel).
- **PNG** and **WebP**: encode without alpha when composited on white (prefer RGB, not RGBA,
  unless template requires otherwise — default RGB on white).
- Automated test: exported PNG pixel at corner (outside content) is white or near-white, not transparent.

### Delivery mode behavior

#### ZIP archive (default)

- Save dialog saves **one file** using
  `CvExportFilenameHelper.SuggestImageZipFilename(first, last)` →
  `{FirstName}_{LastName}_CV_images.zip` (fallback `ReVitae_CV_images.zip`).
- ZIP contains one image per **selected** page at ZIP root (no nested folder in v1):

```text
Jane_Doe_CV_images.zip
├── page-01.png
├── page-02.png
└── page-03.png
```

- Internal ZIP entry names use **short** pattern `page-NN.{ext}` (no person prefix).
- Extension inside ZIP matches selected format (`.png`, `.jpg`, `.webp`).
- Single-page CV → ZIP with one image is valid.
- Use `System.IO.Compression.ZipArchive` — no new ZIP dependency.

#### Separate files

- Open **folder picker** via Avalonia `StorageProvider.OpenFolderPickerAsync`
  (not multi-file save dialog).
- Write images directly into chosen folder using **prefixed** filenames from
  `CvExportFilenameHelper.SuggestImagePageFilename(first, last, pageIndex, format)`:

```text
/Users/jane/exports/
├── Jane_Doe_CV_page-01.png
├── Jane_Doe_CV_page-02.png
└── Jane_Doe_CV_page-03.png
```

- On filename collision, append `-2`, `-3`, … before extension (Core helper —
  never silent overwrite).
- Post-export **Show in folder** reveals folder via first exported page path;
  **Open file** opens first exported page with default image viewer.

### Multi-page rules

- Page count must match QuestPDF PDF page count for the same `CvExportDocument`.
- Page indices are **1-based** in filenames: `page-01`, `page-02`, … (zero-padded
  to two digits minimum; use three digits when page count ≥ 100).
- Long CVs export up to `CvImageExportLimits.MaxPageCount` (**50**) **total PDF pages**.
  Beyond cap → fail with localized error (do not truncate silently).
- Page **range** cannot exceed total pages or cap.
- Use `JohnDoeStressCvDataset` or equivalent multi-page fixture for regression.

### Validation and modal rules

- Same validation gate as **022**: invalid form → export modal never opens.
- **Images card** → show options sub-panel; **do not** call `SetExportModalVisible(false)` yet.
- Cancel / Back on options sub-panel → return to format grid, no save dialog.
- Cancel on save/folder dialog → silent return, no error toast.
- Escape: dismiss options sub-panel first, then export modal (extend existing stack in
  `MainWindow.axaml.cs`).

### Scale & render size (Docnet)

Docnet does **not** accept a DPI parameter — it renders via `PageDimensions(width, height)`
(max side in pixels). Map user scale to render size:

| Scale | Render multiplier | Typical A4 result (approx.) |
| ----- | ----------------- | --------------------------- |
| 1×    | base              | ~794 × 1123 px (72 pt → px) |
| 2×    | 2× base           | ~1588 × 2246 px             |

Compute `base` from PDF page media box (points) converted to pixels; apply scale;
then clamp each side to `CvImageExportLimits.MaxPixelDimension` (**4096**).
If clamping would occur, fail with `ExportImageRasterFailed` (no silent downscale in v1).

Dispose every `SixLabors.ImageSharp.Image` after encode (`using` / `try/finally`).

## Supported Image Formats

| Format | Extension | Encoder                  | Notes                                      |
| ------ | --------- | ------------------------ | ------------------------------------------ |
| PNG    | `.png`    | ImageSharp `PngEncoder`  | Lossless; default; opaque white background |
| JPEG   | `.jpg`    | ImageSharp `JpegEncoder` | Quality slider; no alpha                   |
| WebP   | `.webp`   | ImageSharp `WebpEncoder` | Quality slider; PNG fallback               |

Reuse encoding patterns from `ProfilePhotoStorage` where applicable.

## Architecture

### Rendering pipeline (required)

```text
CvExportDocument
  └─ QuestPdfCvExporter.Export(document) → PDF bytes (existing 16 templates)
       └─ page count + CvImagePageRangeResolver
       └─ ICvPdfPageRasterizer.Rasterize(pdfBytes, scale, pageIndices) → IReadOnlyList<Image>
            └─ CvImageBackgroundCompositor → opaque white RGB
            └─ CvImageEncoder.Encode(page, format, quality) → byte[]
                 └─ ICvImageExportPackager
                      ├─ ZipArchivePackager → single .zip on disk
                      └─ SeparateFilesPackager → folder + collision-safe names
```

### Core types (`src/ReVitae.Core/Export/Images/`)

| Type / file                   | Responsibility                                        |
| ----------------------------- | ----------------------------------------------------- |
| `CvImageExportFormat`         | `Png`, `Jpeg`, `WebP`                                 |
| `CvImageExportDelivery`       | `ZipArchive`, `SeparateFiles`                         |
| `CvImageExportScale`          | `Standard` (1×), `High` (2×)                          |
| `CvImagePageRange`            | All pages or inclusive From/To                        |
| `CvImageExportOptions`        | Format, delivery, quality, scale, page range          |
| `CvImageExportLimits`         | `MaxPageCount = 50`, `MaxPixelDimension = 4096`       |
| `CvImageExportDestination`    | `ZipFile(string path)` or `Folder(string path)`       |
| `CvImagePageRangeResolver`    | Validate range → page index list                      |
| `CvImageExportSizeEstimator`  | Heuristic MB estimate for UI hint                     |
| `CvImageBackgroundCompositor` | White background composite before encode              |
| `ICvPdfPageRasterizer`        | PDF bytes → page images (caller disposes)             |
| `DocnetPdfPageRasterizer`     | Docnet — export limits, no OCR logging                |
| `CvImageEncoder`              | ImageSharp PNG/JPEG/WebP encode                       |
| `CvImageExportFilenameHelper` | ZIP/page naming + collision suffix                    |
| `ICvImageExportPackager`      | Write encoded pages to ZIP or folder                  |
| `CvImageExporter`             | Orchestrates full pipeline → `CvExportResult`         |
| `IImageExportProgress`        | Optional callback: `(currentPage, totalPages, phase)` |

**Rasterizer choice:** use **Docnet.Core** + **SixLabors.ImageSharp** (already in
`ReVitae.Core.csproj`). Do **not** add PDFium or a new PDF rasterization NuGet.
(`SkiaSharp` is already referenced in the UI project for SVG icons — unrelated.)

Extract shared Docnet page-rendering logic only if duplication exceeds ~40 lines;
otherwise keep export rasterizer separate from `Import/Ocr/DocnetPdfPageRenderer`.

### Facade integration (important)

Existing `CvDocumentExporter.Export(..., Stream output)` is **single-stream** and
cannot represent folder delivery. Do **not** force folder export through that API.

Add a dedicated Core entry point:

```csharp
public static class CvImageExporter
{
    public static CvExportResult Export(
        CvExportDocument document,
        CvImageExportOptions options,
        CvImageExportDestination destination,
        IImageExportProgress? progress = null);
}
```

- **ZIP:** `destination = ZipFile(absolutePath)` — writes `.zip` directly to path.
- **Separate files:** `destination = Folder(absolutePath)` — writes `N` files.

`MainWindow` calls `CvImageExporter.Export` after save/folder picker — **not**
`CvDocumentExporter` for image paths. Leave `CvExportFormat.Images` out of
`CvDocumentExporter` switch.

Extend enums/catalog only for the modal card:

```csharp
// CvExportFormat.Images  — catalog + UI routing only
// CvExportFormatCategory.Images
```

### Catalog & assets

Update `CvExportFormatCatalog`:

- Add `Desc(CvExportFormat.Images, CvExportFormatCategory.Images, "images", ...)`.
- Do **not** add `Images` to `GetExtension` / generic save-dialog routing — ZIP
  filename comes from `SuggestImageZipFilename`.
- Add `TranslationKeys.ExportFormatImages`, `ExportFormatImagesHint`,
  `ExportCategoryImages`.

Icon: **`src/ReVitae/Assets/ExportFormats/export-format-images.svg`**
(icon slug `images` → `export-format-{slug}.svg` per `CvExportFormatIconLoader`).

Category order in `PopulateExportFormatCards()`:
`Documents` → `WebAndText` → **`Images`** → `Structured`.

Update `CvExportFormatCatalogEdgeCaseTests` category counts:
`Documents=4`, `WebAndText=4`, **`Images=1`**, `Structured=7`.

### UI layer (`src/ReVitae/`)

| Piece                           | Notes                                                       |
| ------------------------------- | ----------------------------------------------------------- |
| `ExportImageOptionsPanel.axaml` | All options + size estimate + progress + Back/Export        |
| `MainWindow.axaml`              | Sibling panels: format grid vs `ExportImageOptionsPanel`    |
| `MainWindow.ExportImages.cs`    | Options flow, async export, progress, folder picker         |
| `CvExportFilePickerOptions`     | Add `CreateZipSaveOptions(localizer, suggestedFilename)`    |
| `CvExportShellHelper`           | Unchanged — ZIP and first page path work with existing APIs |

Flow:

```text
format != Images  →  existing path (close modal → save dialog → CvDocumentExporter)
format == Images  →  show ExportImageOptionsPanel (modal stays open)
  → user confirms  →  close modal  →  ZIP save OR folder picker  →  CvImageExporter (async + progress)
```

While options panel is visible, disable format card clicks (panel replaces grid).

### Filename and save dialog rules

| Delivery       | Dialog type | Default name / target    | Filter  |
| -------------- | ----------- | ------------------------ | ------- |
| ZIP            | Save file   | `Jane_Doe_CV_images.zip` | `*.zip` |
| Separate files | Open folder | (user picks folder)      | n/a     |

Extend `CvExportSaveDialogDefaults` / `CvExportFilePickerOptions` for ZIP only
(new `ExportZipFileType` key) — not via `CvExportFormat.Images` generic switch.

## Profile photo and templates

Exported page images must match PDF export for the selected template:

- sidebar **initials fallback** when no photo (**023**),
- all **16** template families (`QuestPdfCvExporter` switch),
- Unicode / diacritics render correctly in raster output.

Smoke-test at minimum: **Classic Sidebar** + **Modern Sidebar** + **Clean Top Header**
automated; manual QA on remaining templates best effort.

## Localization

Add EN + SK keys (`TranslationKeys` + `AppLocalizer`):

| Constant                          | Key string                          | English (example)                      |
| --------------------------------- | ----------------------------------- | -------------------------------------- |
| `ExportCategoryImages`            | `export.category.images`            | Images                                 |
| `ExportFormatImages`              | `export.format.images`              | Page images                            |
| `ExportFormatImagesHint`          | `export.format.imagesHint`          | PNG, JPEG, or WebP — one file per page |
| `ExportImageOptionsTitle`         | `export.image.optionsTitle`         | Image export options                   |
| `ExportImageFormatLabel`          | `export.image.formatLabel`          | Image format                           |
| `ExportImageFormatPng`            | `export.image.formatPng`            | PNG (lossless)                         |
| `ExportImageFormatJpeg`           | `export.image.formatJpeg`           | JPEG (smaller file)                    |
| `ExportImageFormatWebp`           | `export.image.formatWebp`           | WebP (modern web)                      |
| `ExportImageDeliveryLabel`        | `export.image.deliveryLabel`        | Delivery                               |
| `ExportImageDeliveryZip`          | `export.image.deliveryZip`          | Single ZIP archive                     |
| `ExportImageDeliveryZipHint`      | `export.image.deliveryZipHint`      | All pages in one `.zip` file           |
| `ExportImageDeliverySeparate`     | `export.image.deliverySeparate`     | Separate files                         |
| `ExportImageDeliverySeparateHint` | `export.image.deliverySeparateHint` | Save each page into a folder           |
| `ExportImageQualityLabel`         | `export.image.qualityLabel`         | Quality                                |
| `ExportImageScaleLabel`           | `export.image.scaleLabel`           | Resolution                             |
| `ExportImageScale1x`              | `export.image.scale1x`              | Standard (1×)                          |
| `ExportImageScale2x`              | `export.image.scale2x`              | High (2×)                              |
| `ExportImagePagesLabel`           | `export.image.pagesLabel`           | Pages                                  |
| `ExportImagePagesAll`             | `export.image.pagesAll`             | All pages                              |
| `ExportImagePagesRange`           | `export.image.pagesRange`           | Page range                             |
| `ExportImagePageFromLabel`        | `export.image.pageFromLabel`        | From                                   |
| `ExportImagePageToLabel`          | `export.image.pageToLabel`          | To                                     |
| `ExportImageRangeInvalid`         | `export.image.rangeInvalid`         | Invalid page range.                    |
| `ExportImageSizeEstimate`         | `export.image.sizeEstimate`         | {0} pages · ~{1} ({2}, {3})            |
| `ExportImageSizeEstimateUnknown`  | `export.image.sizeEstimateUnknown`  | Calculating size…                      |
| `ExportImageProgressRendering`    | `export.image.progressRendering`    | Rendering page {0} of {1}…             |
| `ExportImageProgressWriting`      | `export.image.progressWriting`      | Writing files…                         |
| `ExportImageExportButton`         | `export.image.exportButton`         | Export images                          |
| `ExportImageBackButton`           | `export.image.backButton`           | Back to formats                        |
| `ExportImageOptionsRequired`      | `export.image.optionsRequired`      | Image export options are missing.      |
| `ExportImageTooManyPages`         | `export.image.tooManyPages`         | Too many pages (max {0}).              |
| `ExportImageRasterFailed`         | `export.image.rasterFailed`         | Could not render CV pages as images.   |
| `ExportZipFileType`               | `export.zipFileType`                | ZIP archive                            |
| `ExportFolderPickerTitle`         | `export.folderPickerTitle`          | Choose folder for CV images            |
| `ExportedImagesToZip`             | `export.exportedImagesToZip`        | Exported {0} page image(s) to {1}.     |
| `ExportedImagesToFolder`          | `export.exportedImagesToFolder`     | Exported {0} page image(s) to folder.  |

Reuse `ExportOpenFile`, `ExportShowInFolder`, `ExportFailed`, `ExportFailedFormat`.

Register new constants in `TranslationKeys.All` (project convention).

## Testing (exhaustive edge-case coverage required)

The prompt is **not complete** unless every scenario in **Part 9A–9H** has a
corresponding automated test. Prefer focused `[Theory]` / `[InlineData]` cases over
skipping listed rows.

Target **70+** new tests; update README test badge after implementation.
Full `./scripts/test.sh` and `npm run lint` must pass.

**Exclude** `CvExportFormat.Images` from `CvDocumentExporterEdgeCaseTests.Export_AllShippedFormats_*`
(generic single-stream loop). Cover Images only via `CvImageExporterTests` and Part 9.

### Test layout (required files)

Under `tests/ReVitae.Tests/Export/Images/`:

| File                                         | Min tests | Responsibility                                               |
| -------------------------------------------- | --------- | ------------------------------------------------------------ |
| `CvImageExportFilenameHelperTests.cs`        | 10+       | ZIP name, page names, collision, unicode, padding            |
| `CvImagePageRangeResolverTests.cs`           | 12+       | All pages, partial range, invalid range, single page         |
| `CvImageExportSizeEstimatorTests.cs`         | 8+        | ordering by format/quality/scale; monotonic page count       |
| `CvImageBackgroundCompositorTests.cs`        | 6+        | white corners; JPEG path opaque                              |
| `CvImageEncoderTests.cs`                     | 12+       | PNG/JPEG/WebP bytes; quality size delta; WebP fallback       |
| `CvImageExportZipPackagerTests.cs`           | 12+       | entries, root layout, empty input guard                      |
| `CvImageExportSeparateFilesPackagerTests.cs` | 10+       | prefixed names, collision `-2`, folder create                |
| `DocnetPdfPageRasterizerTests.cs`            | 8+        | page count, dimensions, scale, dispose                       |
| `CvImageExporterTests.cs`                    | 14+       | end-to-end ZIP/folder, range, failures, progress callback    |
| `CvImageExportLimitsTests.cs`                | 6+        | cap at 50 pages, max dimension fail                          |
| `CvImageExportOcrRoundTripSmokeTests.cs`     | 4+        | export PNG → non-empty file, min dimensions, importable size |
| `CvImageExportTemplateSmokeTests.cs`         | 6+        | 3 templates × page count parity with PDF                     |

Extend `CvExportTestFixtures` if needed (multi-page document builder).

Fixtures:

- `CvExportTestFixtures` — representative and multi-page `CvExportDocument`,
- `tests/ReVitae.Tests/Import/MinimalPdfWriter.cs` — tiny PDF for rasterizer unit tests,
- `JohnDoeStressCvDataset` / minimal architect dataset — long CV page cap tests,
- QuestPDF-generated PDF for page-count parity assertions.

### Part 9A - Filename helper

| Scenario                                    | Expected outcome             |
| ------------------------------------------- | ---------------------------- |
| `SuggestImageZipFilename` with first + last | `Jane_Doe_CV_images.zip`     |
| Missing names                               | `ReVitae_CV_images.zip`      |
| Invalid path chars in names                 | sanitized                    |
| Unicode in names                            | preserved                    |
| `SuggestImagePageFilename` page 1           | `Jane_Doe_CV_page-01.png`    |
| Page index 10                               | `page-10` (two-digit pad)    |
| Page index 100                              | `page-100` (three-digit pad) |
| Collision `page-01.png` exists              | writes `page-01-2.png`       |
| JPEG extension                              | `.jpg` not `.jpeg`           |
| WebP extension                              | `.webp`                      |

### Part 9B - Page range resolver

| Scenario                                     | Expected outcome                         |
| -------------------------------------------- | ---------------------------------------- |
| All pages, 5 total                           | `[1,2,3,4,5]`                            |
| Range 2–3 of 5                               | `[2,3]`                                  |
| Range 1–1                                    | `[1]`                                    |
| From > To                                    | validation error                         |
| From = 0 or To = 0                           | validation error                         |
| To > total pages                             | validation error                         |
| Range beyond `MaxPageCount` total PDF        | `ExportImageTooManyPages` at export time |
| Filenames use original indices for range 2–3 | `page-02`, `page-03` in output           |

### Part 9C - Size estimator

| Scenario                     | Expected outcome               |
| ---------------------------- | ------------------------------ |
| More pages → larger estimate | strict increase                |
| JPEG 70 vs 95 at same pages  | 95 ≥ 70 (bytes heuristic)      |
| PNG vs JPEG same pages       | PNG ≥ JPEG (typical)           |
| 2× vs 1× scale               | 2× ≥ 1×                        |
| Zero pages                   | safe fallback string / zero MB |
| Unknown page count           | does not throw                 |

### Part 9D - Background compositor & encoder

| Scenario                       | Expected outcome                          |
| ------------------------------ | ----------------------------------------- |
| Raster with transparent margin | corner pixel white/opaque after composite |
| PNG encode after composite     | valid PNG header, non-empty               |
| JPEG encode                    | no alpha channel                          |
| WebP encode success            | valid WebP                                |
| WebP encode throws             | fallback PNG bytes                        |
| Quality 70 vs 95 JPEG          | 95 file size ≥ 70 (sanity)                |
| Null/empty image input         | failure without throw                     |

### Part 9E - Rasterizer

| Scenario                                | Expected outcome             |
| --------------------------------------- | ---------------------------- |
| Minimal 1-page PDF                      | 1 image, width/height > 0    |
| 3-page PDF                              | 3 images                     |
| Scale 2× vs 1×                          | 2× dimensions ≥ 1×           |
| Selected page indices only              | returns only requested pages |
| Empty PDF bytes                         | failure                      |
| Corrupt PDF bytes                       | failure, no crash            |
| Images disposed after export            | no leak in loop test         |
| Exceeds `MaxPixelDimension` after scale | `ExportImageRasterFailed`    |

### Part 9F - ZIP packager

| Scenario                      | Expected outcome               |
| ----------------------------- | ------------------------------ |
| 1 page PNG                    | valid ZIP, one entry at root   |
| 3 pages                       | `page-01`…`page-03` entries    |
| Range 2–3 only                | two entries with correct names |
| Empty page list               | failure, no empty ZIP          |
| Entry extensions match format | `.jpg` for JPEG                |
| ZIP readable by `ZipArchive`  | central directory valid        |

### Part 9G - Separate files packager

| Scenario                     | Expected outcome            |
| ---------------------------- | --------------------------- |
| Writes N files to folder     | count matches               |
| Prefixed filenames           | `{Name}_CV_page-NN.ext`     |
| Collision handling           | `-2` suffix                 |
| Read-only folder / I/O error | failure result              |
| Range export                 | only selected pages on disk |

### Part 9H - End-to-end exporter & integration

| Scenario                  | Expected outcome                                                                                                                                                           |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ZIP happy path PNG 2×     | success, non-empty zip                                                                                                                                                     |
| Folder happy path JPEG    | success, files exist                                                                                                                                                       |
| WebP delivery             | valid files                                                                                                                                                                |
| Page range 1–1 only       | single output file                                                                                                                                                         |
| Null document             | failure                                                                                                                                                                    |
| Progress callback invoked | once per page + writing phase                                                                                                                                              |
| Total pages > 50          | `ExportImageTooManyPages`                                                                                                                                                  |
| Profile photo document    | export succeeds (smoke)                                                                                                                                                    |
| Diacritics in name        | filename preserved                                                                                                                                                         |
| Deterministic re-export   | same bytes for same input (PNG lossless)                                                                                                                                   |
| OCR round-trip smoke      | exported PNG: width ≥ 400, height ≥ 400, size ≥ 10 KB; optional import via `CvDocumentImporter` on raster image does not throw (best effort — full OCR quality is **032**) |

### Regression

- All **15** existing `CvDocumentExporter` format tests still pass unchanged.
- `CvExportFormatCatalogEdgeCaseTests` updated for **Images** category.
- Profile photo PDF/HTML export tests unaffected.

## Documentation (full pass required)

Review and update **every** file below. The prompt is **not complete** if any row is
left stale after implementation.

| File                                    | Required updates                                                                                                                                                                                                      |
| --------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docs/export-formats.md`                | **Images** section: formats, delivery, page range, scale, limits, white background, pipeline diagram; remove raster from Exclusions; note image ZIP ≠ multi-format bundle; link to import OCR for PNG/JPEG **import** |
| `docs/concept.md`                       | Phase 1 export list includes page images; remove “future raster export” wording                                                                                                                                       |
| `docs/import-formats.md`                | One cross-link: exported page images can be re-imported via OCR (**032**), not structured import                                                                                                                      |
| `README.md`                             | Highlights: image export (PNG/JPEG/WebP, ZIP or folder, page range); **16** export choices; mermaid flow optional branch for Images options step; prompts `001–031`; test badge count                                 |
| `CHANGELOG.md`                          | Unreleased **Added**: prompt **031** with feature bullets                                                                                                                                                             |
| `prompts/022-multi-format-cv-export.md` | Add footnote in Exclusions: raster export added by **031** (do not edit acceptance #13 text — note supersession in CHANGELOG only)                                                                                    |
| `prompts/034-cv-quality-hints.md`       | No change required unless export summary mentions format count                                                                                                                                                        |

Documentation must describe:

- two-step export modal flow (Images options before save/folder),
- page range semantics and filename indexing,
- size estimate as heuristic,
- export progress behavior,
- opaque white background policy,
- `MaxPageCount` / dimension limits,
- symmetry: export PNG ≠ import structured JSON; image **import** = OCR path.

## Acceptance Criteria

1. Export modal includes **Images** card (category, icon, EN + SK labels).
2. Options panel: format, delivery, quality, scale, **page range**, **size estimate**,
   **progress** during export.
3. **ZIP** → `page-NN.{ext}` at archive root for each **selected** page.
4. **Separate files** → folder picker + prefixed names, collision-safe.
5. **Page range** exports only selected pages; filenames keep original page numbers.
6. **Opaque white** background on all outputs; JPEG never transparent.
7. Template-aligned smoke tests for Classic Sidebar, Modern Sidebar, Clean Top Header.
8. Validation gate unchanged; async export does not freeze UI.
9. Post-export **Open file** / **Show in folder** work for ZIP and folder modes.
10. `CvImageExporter` owns orchestration in Core.
11. **Part 9A–9H** edge-case matrix fully covered; **70+** new tests; lint + full suite green.
12. **Documentation table** fully updated; CHANGELOG entry added.

## Manual QA Checklist

1. Valid CV → Images → PNG + ZIP + all pages → unzip → white background, diacritics OK.
2. JPEG quality 80 → smaller than PNG; size estimate decreases vs PNG.
3. WebP → valid in viewer.
4. Separate files → folder → prefixed files.
5. Page range 2–3 only → two files named `page-02`, `page-03`.
6. Single-page CV → one file.
7. Multi-page CV → count matches PDF; progress text increments.
8. Template switch → output reflects template.
9. With / without profile photo.
10. Invalid form → Export blocked.
11. Invalid range → Export images disabled + inline message.
12. Back / × / Escape behavior.
13. Dark theme readable.
14. Stress CV near 50 pages → success with progress; 51+ → error.
15. Long export: UI responsive, no duplicate export on double-click.

## Suggested Implementation Order

1. Core types: options, page range, limits, destination, filename helpers.
2. `CvImagePageRangeResolver`, `CvImageExportSizeEstimator`, `CvImageBackgroundCompositor`.
3. `DocnetPdfPageRasterizer` + `CvImageEncoder` (PNG first).
4. Packagers (ZIP + separate files).
5. `CvImageExporter` + progress callback.
6. Catalog, icon, localization (EN + SK).
7. `ExportImageOptionsPanel` + async flow in `MainWindow.ExportImages.cs`.
8. Page range UI + live size estimate binding.
9. Progress UI + disable controls during export.
10. Full Part 9 test matrix + documentation pass.

## Regression Guards

- All **15** existing `CvDocumentExporter` formats unchanged.
- `CvExportFormatCatalog.GetEnabledFormats()` count → **16**.
- Export modal Escape stack manually verified.
- Profile photo / PDF export tests unaffected.
- No new network dependencies.

## Future Work (Not This Prompt)

- **040** — AI-assisted import,
- Remember-last image options when project save/load lands,
- Nested folder inside ZIP (`Jane_Doe_CV/page-01.png`),
- Cancel button during long export,
- Exact byte-size preview (replace heuristic estimator).

## Expected Result

ReVitae users export CV pages as **PNG, JPEG, or WebP**, choosing **ZIP** or
**separate files**, **all pages or a range**, with **size guidance** and **progress**
feedback. Output matches template-aligned PDF layout on an **opaque white** background,
supports multi-page CVs, and fits the validation-gated export modal — without AI or
cloud conversion.
