# CV export formats

ReVitae exports CVs after validation passes and the user picks a format in the
**Export** modal. Visual formats consume `CvExportDocument` (template-aware preview
projection); structured formats consume `CvExportSourceData` (core CV models for
round-trip interchange). **Page images** use a dedicated two-step flow in the same
modal (options panel → save or folder picker).

See also: [`import-formats.md`](import-formats.md) (symmetric import),
[`revitae-project-json.md`](revitae-project-json.md) (native JSON schema).

## User flow

### Standard formats (PDF, DOCX, JSON, …)

1. Fill valid CV data and choose a preview template.
2. Click **Export** (validation runs first; invalid forms never open the modal).
3. Pick a format card (official file-type icon + localized label).
4. Save dialog opens with the correct extension, filename, and file-type filter.
5. File is written locally; success text appears with optional **Open file** /
   **Show in folder** actions.

### Page images (PNG / JPEG / WebP)

1. Same validation gate as above.
2. Pick the **Page images** card under category **Images**.
3. Configure **image format**, **delivery** (ZIP or separate files), **quality**
   (JPEG/WebP), **scale** (1× / 2×), and **pages** (all or range) in the options
   panel. A **size estimate** hint updates as options change.
4. Click **Export images** → save dialog (ZIP) or **folder picker** (separate files).
5. Progress text appears in the status area while pages render (`Rendering page X of Y…`).
6. Success message + **Open file** / **Show in folder** (ZIP path or first page file).

Post-export shell actions use `CvExportShellHelper` in the UI layer; path
validation uses `CvExportPathHelper` in Core.

## Supported formats (16)

| Category   | Format       | Extension(s)           | Writer / notes                                      |
| ---------- | ------------ | ---------------------- | --------------------------------------------------- |
| Documents  | PDF          | `.pdf`                 | QuestPDF (`ICvPdfExporter`) — template-aligned A4   |
| Documents  | Word         | `.docx`                | Open XML via `CvVisualExportWriter`                 |
| Documents  | OpenDocument | `.odt`                 | ODF ZIP + `content.xml`                             |
| Documents  | Rich Text    | `.rtf`                 | RTF with Unicode                                    |
| Web & text | HTML         | `.html`                | Self-contained HTML + embedded CSS                  |
| Web & text | Markdown     | `.md`                  | Semantic headings                                   |
| Web & text | Plain text   | `.txt`                 | Readable section blocks                             |
| Web & text | LaTeX        | `.tex`                 | Compilable `article` stub                           |
| **Images** | Page images  | `.zip` or page files   | `CvImageExporter` — see below                       |
| Structured | ReVitae JSON | `.revitae.json`        | v1 text-only; v2 adds optional embedded photo       |
| Structured | JSON Resume  | `.json`                | Subset compatible with `JsonResumeMapper`           |
| Structured | YAML         | `.yaml`                | JSON-equivalent tree (quoted scalars for `#`, `+`)  |
| Structured | Europass XML | `_europass.xml` suffix | Europass namespace                                  |
| Structured | HR-XML       | `_hrxml.xml` suffix    | HR-XML-like resume nodes                            |
| Structured | CSV          | `.csv`                 | Header + single personal row (mirrors import limit) |
| Structured | TSV          | `.tsv`                 | Tab-delimited personal row                          |

Filename defaults come from `CvExportFilenameHelper.SuggestFilename(first, last, format)`.
Image ZIP names use `SuggestImageZipFilename` → `{First}_{Last}_CV_images.zip`.
JSON/XML variants disambiguate via extension or suffix (see catalog in
`CvExportFormatCatalog`).

## Page image export

| Option       | Values                      | Default |
| ------------ | --------------------------- | ------- |
| Image format | PNG, JPEG, WebP             | PNG     |
| Delivery     | ZIP archive, Separate files | ZIP     |
| Quality      | 70–100 (JPEG/WebP)          | 90      |
| Scale        | 1×, 2×                      | 2×      |
| Pages        | All pages, From–To range    | All     |

**ZIP delivery:** one `{Name}_CV_images.zip` containing `page-01.{ext}`, `page-02.{ext}`, …
at archive root (original page numbers preserved when using a range).

**Separate files:** `{Name}_CV_page-01.{ext}` written into the chosen folder;
collision-safe `-2`, `-3` suffixes.

**Pipeline:**

```text
CvExportDocument
  └─ QuestPdfCvExporter → PDF bytes
       └─ DocnetPdfPageRasterizer (scale, page range)
            └─ CvImageBackgroundCompositor (opaque white)
                 └─ CvImageEncoder (PNG / JPEG / WebP)
                      └─ ZIP or folder packager
```

**Limits:** max **50** PDF pages; max **4096** px per side after scaling. Output uses
an **opaque white** background (print-like pages, not transparent PNG overlays).

**Import symmetry:** exported PNG/JPEG images can be **re-imported via OCR** (prompt
**032**), not as structured CV data. See [`import-formats.md`](import-formats.md).

The page-image ZIP contains multiple files of **one image format** — it is not a
multi-format document bundle.

## Profile photo in visual exports

When a profile photo is uploaded in the form, `BuildExportDocument()` sets
`CvExportDocument.PhotoPath` to the local stored copy:

| Format              | With photo                                  | Without photo                                                                |
| ------------------- | ------------------------------------------- | ---------------------------------------------------------------------------- |
| PDF (all templates) | Embedded image in template slot             | Sidebar templates show **initials avatar**; Clean Top Header stays text-only |
| HTML                | `<img>` data URI in header/sidebar region   | No photo block                                                               |
| DOCX                | Inline image after name block               | No image                                                                     |
| ODT                 | Best-effort (no dedicated photo slot in v1) | —                                                                            |
| **Page images**     | Embedded in rasterized PDF pages            | Initials in sidebar templates                                                |

Source images: JPEG/PNG/WebP up to **15 MB**; WebP is transcoded to JPEG in
`ProfilePhotoStorage` for downstream writers. JPEG uploads are EXIF-orientation
normalized on save.

Structured ReVitae JSON/YAML export never writes absolute `profilePhotoPath`.
When a photo exists, export emits **`revitaeVersion: 2`** with
`profilePhotoBase64` and `profilePhotoContentType` inside `personalInformation`.
Text-only exports remain at version 1.

## Architecture

```text
MainWindow (validate → Export modal → save/folder dialog)
    ├─ BuildExportDocument() → CvExportDocument
    ├─ BuildExportSourceData() → CvExportSourceData
    ├─ CvDocumentExporter.Export(document, source, format, stream)  [15 formats]
    └─ CvImageExporter.Export(document, options, destination)       [page images]
           ├─ Visual: CvVisualExportWriter
           ├─ PDF: QuestPdfCvExporter
           └─ Structured: CvStructuredExportWriter
```

Catalog metadata (`CvExportFormatCatalog`, `CvExportFormatDescriptor`) drives the
modal UI and save-dialog defaults (`CvExportSaveDialogDefaults`). Icons live under
`src/ReVitae/Assets/ExportFormats/` and load through `CvExportFormatIconLoader`.

## Exclusions

- Cloud upload / share sheets
- Password-protected output
- ZIP bundles of **multiple document formats** (page-image ZIP is allowed)
- TIFF, HEIC, BMP, single stitched poster image

## Tests

Export coverage lives under `tests/ReVitae.Tests/Export/` including facade routing,
catalog, filename helper, save-dialog defaults, per-format smoke checks, structured
round-trip tests (`ExportImportRoundTripTests`), profile-photo export paths, and
**image export** under `tests/ReVitae.Tests/Export/Images/` (**81+** tests).

Profile photo storage and initials logic are covered in
`tests/ReVitae.Tests/ProfilePhotoStorageTests.cs` and
`ProfilePhotoInitialsTests.cs`. The full suite currently runs **1273** tests via
`dotnet test`.
