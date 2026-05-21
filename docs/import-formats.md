# CV import formats

This document describes how ReVitae routes CV files through `CvDocumentImporter`,
what users can expect from each category, and non‑goals / security boundaries.

## Format matrix

Detection happens in `CvImportFormatDetector` (extension first; `.json` / `.xml`
also sniff structure). Routing is implemented by dedicated `ICvFormatImporter`
implementations registered from `CvDocumentImporter`.

| User-facing types   | Detection                                             | Pipeline                                                              |
| ------------------- | ----------------------------------------------------- | --------------------------------------------------------------------- |
| PDF                 | `.pdf`                                                | PdfPig → quality gate → OCR fallback → text pipeline                  |
| Raster images       | `.jpg`, `.png`, `.webp`, …                            | OCR → text pipeline                                                   |
| Plain text          | `.txt`                                                | Encoding probe → text pipeline                                        |
| Markdown            | `.md`, `.markdown`                                    | Markdig plaintext → text pipeline                                     |
| HTML                | `.html`, `.htm`                                       | HtmlAgilityPack → text pipeline (+ hyperlink hints)                   |
| LaTeX               | `.tex`                                                | LaTeX-oriented extractor → text pipeline                              |
| DOCX                | `.docx`                                               | Open XML paragraphs (+ hyperlink harvest) → text pipeline             |
| Legacy Word         | `.doc`                                                | NPOI → text pipeline                                                  |
| ODT                 | `.odt`                                                | ODT ZIP → secure XML → text pipeline                                  |
| RTF                 | `.rtf`                                                | RtfPipe → text pipeline                                               |
| AbiWord             | `.abw`                                                | Secure XML parse → text pipeline                                      |
| Apple Pages         | `.pages`                                              | ZIP/text sniff → text pipeline                                        |
| WPS                 | `.wps`                                                | Structured sniff → text pipeline                                      |
| JSON Resume         | `.json` (shape sniff: `basics`, `work`, `$schema`, …) | `JsonResumeMapper`                                                    |
| ReVitae native JSON | `*.revitae.json` **or** JSON root `revitaeVersion`    | `ReVitaeJsonMapper`                                                   |
| YAML CV             | `.yaml`, `.yml`                                       | Sniff native vs JSON Resume flavor → mapper bridge                    |
| CSV / TSV tabular   | `.csv`, `.tsv`                                        | `TabularCvMapper` (header + first row → mostly personal fields)       |
| Europass XML        | `.xml` (namespace / token sniff)                      | `EuropassXmlMapper`                                                   |
| HR‑XML-like         | `.xml` (resume / candidate tokens)                    | `HrXmlMapper`                                                         |
| Unknown             | anything else                                         | Error: unsupported format                                             |

Structured formats (`JsonResumeMapper`, `ReVitaeJsonMapper`, Europass / HR‑XML,
tabular) populate `CvImportResult` directly when recognition succeeds.

### Profile photos

- **ReVitae JSON/YAML v2:** optional `profilePhotoBase64` +
  `profilePhotoContentType` in `personalInformation` are decoded, normalized, and
  saved under `%LocalAppData%/ReVitae/profile-photos/`. Invalid base64 skips the
  photo but keeps text import.
- **ReVitae JSON v1:** no photo fields; imports behave as before.
- **Document/text imports** (PDF, DOCX, HTML, Markdown, …): **no photo
  extraction** — the profile photo area stays empty until the user uploads manually.
- **HEIC/HEIF:** not accepted by the upload picker (document as a known limitation).

Text pipelines (`CvTextImportPipeline`) mirror legacy PDF import: normalize →
segment → extract fields.

### Education field extraction (text pipeline)

`CvImportFieldExtractor.ExtractEducation` applies education-specific block
splitting and merging on top of the generic blank-line segmentation:

- **Continuation merge:** blocks that look like mid-phrase institution
  continuations (lines starting with `and …`, orphan field fragments such as
  `Engineering` after `High School of Electrical`) are merged back into the
  preceding entry instead of becoming a second education row.
- **Multi-line institution names:** header lines that form one school name are
  joined (for example `Faculty of` + `Information Technology`).
- **Classic layouts preserved:** explicit degree + university pairs, and
  degree + field + institution triples, are still mapped to separate fields and
  are not over-merged.

Regression coverage lives in `tests/ReVitae.Tests/Import/EducationImportEdgeCaseTests.cs`.

## Limits

- **Maximum file size:** `CvImportLimits.MaxFileBytes` (**25 MiB inclusive**).
  Larger payloads fail fast with `TranslationKeys.ImportErrorFileTooLarge`
  before extractors run.

## XML security (XXE)

Office-derived XML paths use `SecureXmlReaderFactory`:

- `DtdProcessing.Prohibit`
- `XmlResolver = null`

Untrusted `.xml`, ODT content streams, AbiWord payloads, etc. must never expand
external entities. Attempting classic XXE payloads results in `XmlException`
rather than network or file disclosure.

## Exclusions & caveats

- **Image-only PDFs:** when PdfPig text fails the quality gate, ReVitae attempts
  local OCR (Tesseract) if installed. Requires `tessdata` (bundled or under
  `%LocalAppData%/ReVitae/tessdata/`). OCR reading order may not preserve
  multi-column layout — prefer `*.revitae.json` for round-trip.
- **Raster images:** JPEG, PNG, WebP, TIFF, and BMP import via OCR when Tesseract
  is available; otherwise `ImportErrorOcrUnavailable`.
- **Password-protected Office/PDF:** rejected where libraries surface encryption.
- **Perfect fidelity:** imports are **draft data**. Complex layouts (multi-column,
  text boxes, drawings) may collapse ordering or lose sidebar context.
- **Tabular CSV/TSV:** only the **first data row** after the header is mapped;
  extra rows emit `TranslationKeys.ImportWarningTabularMultipleRowsIgnored`.
- **Unknown JSON:** JSON files that do not match Json Resume / native ReVitae
  shapes are treated as **unsupported format** at detection time (no importer).
- **Unknown XML:** Generic XML without Europass / HR‑XML heuristics maps to
  **unsupported format**.

## Related docs

- Export formats: [`export-formats.md`](export-formats.md)
- Native interchange schema: [`revitae-project-json.md`](revitae-project-json.md)
- Product prompt and rationale: [`../prompts/021-multi-format-cv-import.md`](../prompts/021-multi-format-cv-import.md)
- Profile photo upload and template integration: [`../prompts/023-profile-picture-upload.md`](../prompts/023-profile-picture-upload.md)
