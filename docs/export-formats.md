# CV export formats

ReVitae exports CVs through `CvDocumentExporter` after validation passes and the
user picks a format in the **Export** modal. Visual formats consume
`CvExportDocument` (template-aware preview projection); structured formats consume
`CvExportSourceData` (core CV models for round-trip interchange).

See also: [`import-formats.md`](import-formats.md) (symmetric import),
[`revitae-project-json.md`](revitae-project-json.md) (native JSON schema).

## User flow

1. Fill valid CV data and choose a preview template.
2. Click **Export** (validation runs first; invalid forms never open the modal).
3. Pick a format card (official file-type icon + localized label).
4. Save dialog opens with the correct extension, filename, and file-type filter.
5. File is written locally; success text appears with optional **Open file** /
   **Show in folder** actions.

Post-export shell actions use `CvExportShellHelper` in the UI layer; path
validation uses `CvExportPathHelper` in Core.

## Supported formats (15)

| Category      | Format        | Extension(s)               | Writer / notes                                      |
| ------------- | ------------- | -------------------------- | --------------------------------------------------- |
| Documents     | PDF           | `.pdf`                     | QuestPDF (`ICvPdfExporter`) — template-aligned A4   |
| Documents     | Word          | `.docx`                    | Open XML via `CvVisualExportWriter`                 |
| Documents     | OpenDocument  | `.odt`                     | ODF ZIP + `content.xml`                             |
| Documents     | Rich Text     | `.rtf`                     | RTF with Unicode                                    |
| Web & text    | HTML          | `.html`                    | Self-contained HTML + embedded CSS                  |
| Web & text    | Markdown      | `.md`                      | Semantic headings                                   |
| Web & text    | Plain text    | `.txt`                     | Readable section blocks                             |
| Web & text    | LaTeX         | `.tex`                     | Compilable `article` stub                           |
| Structured    | ReVitae JSON  | `.revitae.json`            | v1 text-only; v2 adds optional embedded photo       |
| Structured    | JSON Resume   | `.json`                    | Subset compatible with `JsonResumeMapper`           |
| Structured    | YAML          | `.yaml`                    | JSON-equivalent tree (quoted scalars for `#`, `+`)  |
| Structured    | Europass XML  | `_europass.xml` suffix     | Europass namespace                                  |
| Structured    | HR-XML        | `_hrxml.xml` suffix        | HR-XML-like resume nodes                            |
| Structured    | CSV           | `.csv`                     | Header + single personal row (mirrors import limit) |
| Structured    | TSV           | `.tsv`                     | Tab-delimited personal row                          |

Filename defaults come from `CvExportFilenameHelper.SuggestFilename(first, last, format)`.
JSON/XML variants disambiguate via extension or suffix (see catalog in
`CvExportFormatCatalog`).

## Profile photo in visual exports

When a profile photo is uploaded in the form, `BuildExportDocument()` sets
`CvExportDocument.PhotoPath` to the local stored copy:

| Format | With photo | Without photo |
| ------ | ---------- | ------------- |
| PDF (all templates) | Embedded image in template slot | Sidebar templates show **initials avatar**; Clean Top Header stays text-only |
| HTML | `<img>` data URI in header/sidebar region | No photo block |
| DOCX | Inline image after name block | No image |
| ODT | Best-effort (no dedicated photo slot in v1) | — |

Source images: JPEG/PNG/WebP up to **15 MB**; WebP is transcoded to JPEG in
`ProfilePhotoStorage` for downstream writers. JPEG uploads are EXIF-orientation
normalized on save.

Structured ReVitae JSON/YAML export never writes absolute `profilePhotoPath`.
When a photo exists, export emits **`revitaeVersion: 2`** with
`profilePhotoBase64` and `profilePhotoContentType` inside `personalInformation`.
Text-only exports remain at version 1.

## Architecture

```text
MainWindow (validate → Export modal → save dialog)
    ├─ BuildExportDocument() → CvExportDocument
    ├─ BuildExportSourceData() → CvExportSourceData
    └─ CvDocumentExporter.Export(document, source, format, stream)
           ├─ Visual: CvVisualExportWriter (TXT, MD, HTML, RTF, LaTeX, DOCX, ODT)
           ├─ PDF: QuestPdfCvExporter
           └─ Structured: CvStructuredExportWriter
```

Catalog metadata (`CvExportFormatCatalog`, `CvExportFormatDescriptor`) drives the
modal UI and save-dialog defaults (`CvExportSaveDialogDefaults`). Icons live under
`src/ReVitae/Assets/ExportFormats/` and load through `CvExportFormatIconLoader`.

## Exclusions

- Raster/image export (PNG, JPEG, …)
- Cloud upload / share sheets
- Password-protected output
- ZIP bundles of multiple formats

## Tests

Export coverage lives under `tests/ReVitae.Tests/Export/` including facade routing,
catalog, filename helper, save-dialog defaults, per-format smoke checks, structured
round-trip tests (`ExportImportRoundTripTests`), and profile-photo export paths
(`CvStructuredExportWriterPhotoTests`, extensions to `CvDocumentExporterEdgeCaseTests`).

Profile photo storage and initials logic are covered in
`tests/ReVitae.Tests/ProfilePhotoStorageTests.cs` and
`ProfilePhotoInitialsTests.cs`. The full suite currently runs **783** tests via
`dotnet test`.
