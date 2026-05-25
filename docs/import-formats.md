# CV import formats

This document describes how ReVitae routes CV files through `CvDocumentImporter`,
what users can expect from each category, and non‑goals / security boundaries.

## Getting started

On first launch (or after **New CV**), ReVitae offers three entry points:

- **Create new CV** — empty structured form with live preview.
- **Import existing CV** — local file routed through the format matrix below.
- **Open saved project** — load a `*.revitae.json` file from disk or recent list.

![Welcome onboarding — create, import, or open a saved project](img/welcome-onboarding-modal.png)

## Format matrix

Detection happens in `CvImportFormatDetector` (extension first; `.json` / `.xml`
also sniff structure). Routing is implemented by dedicated `ICvFormatImporter`
implementations registered from `CvDocumentImporter`.

| User-facing types   | Detection                                             | Pipeline                                                        |
| ------------------- | ----------------------------------------------------- | --------------------------------------------------------------- |
| PDF                 | `.pdf`                                                | PdfPig → quality gate → OCR fallback → text pipeline            |
| Raster images       | `.jpg`, `.png`, `.webp`, …                            | OCR → text pipeline                                             |
| Plain text          | `.txt`                                                | Encoding probe → text pipeline                                  |
| Markdown            | `.md`, `.markdown`                                    | Markdig plaintext → text pipeline                               |
| HTML                | `.html`, `.htm`                                       | HtmlAgilityPack → text pipeline (+ hyperlink hints)             |
| LaTeX               | `.tex`                                                | LaTeX-oriented extractor → text pipeline                        |
| DOCX                | `.docx`                                               | Open XML paragraphs (+ hyperlink harvest) → text pipeline       |
| Legacy Word         | `.doc`                                                | NPOI → text pipeline                                            |
| ODT                 | `.odt`                                                | ODT ZIP → secure XML → text pipeline                            |
| RTF                 | `.rtf`                                                | RtfPipe → text pipeline                                         |
| AbiWord             | `.abw`                                                | Secure XML parse → text pipeline                                |
| Apple Pages         | `.pages`                                              | ZIP/text sniff → text pipeline                                  |
| WPS                 | `.wps`                                                | Structured sniff → text pipeline                                |
| JSON Resume         | `.json` (shape sniff: `basics`, `work`, `$schema`, …) | `JsonResumeMapper`                                              |
| ReVitae native JSON | `*.revitae.json` **or** JSON root `revitaeVersion`    | `ReVitaeJsonMapper`                                             |
| YAML CV             | `.yaml`, `.yml`                                       | Sniff native vs JSON Resume flavor → mapper bridge              |
| CSV / TSV tabular   | `.csv`, `.tsv`                                        | `TabularCvMapper` (header + first row → mostly personal fields) |
| Europass XML        | `.xml` (namespace / token sniff)                      | `EuropassXmlMapper`                                             |
| HR‑XML-like         | `.xml` (resume / candidate tokens)                    | `HrXmlMapper`                                                   |
| Unknown             | anything else                                         | Error: unsupported format                                       |

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
  is available; otherwise `ImportErrorOcrUnavailable`. CV **page images exported**
  from ReVitae re-enter through this OCR path — not structured import.
- **Password-protected Office/PDF:** rejected where libraries surface encryption.
- **Perfect fidelity:** imports are **draft data**. Complex layouts (multi-column,
  text boxes, drawings) may collapse ordering or lose sidebar context.
- **Tabular CSV/TSV:** only the **first data row** after the header is mapped;
  extra rows emit `TranslationKeys.ImportWarningTabularMultipleRowsIgnored`.
- **Unknown JSON:** JSON files that do not match Json Resume / native ReVitae
  shapes are treated as **unsupported format** at detection time (no importer).
- **Unknown XML:** Generic XML without Europass / HR‑XML heuristics maps to
  **unsupported format**.

## AI-assisted fallback

When deterministic text import **fails** or returns a **thin / low-confidence**
draft, ReVitae can offer optional **batched AI extraction**.
This applies only to **text routes** — structured mapper success with enough
sections skips AI automatically.

- Input to the model is **normalized plain text** only (no PDF/image bytes in v1).
- User must **review and Apply** — the form is not changed silently.
- Profile photos are **not** extracted from documents; see [`ai-import.md`](ai-import.md).
- A successful **ReVitae PDF round-trip** usually yields enough populated sections
  that the AI banner does not appear.

## ReVitae PDF round-trip

Deterministic re-import of **PDFs exported from ReVitae** (QuestPDF templates with a
text layer) — without OCR or AI when the text layer is usable.

### Export fingerprint

Each QuestPDF export writes PDF Info metadata readable by PdfPig:

| Key        | Example                  |
| ---------- | ------------------------ |
| `Producer` | `ReVitae`                |
| `Creator`  | `ReVitae/0.2.0`          |
| `Keywords` | `template:ModernSidebar` |

Import uses metadata first, then layout heuristics (`ReVitaePdfExportHints`,
`ReVitaePdfLayoutProfile` per `CvExportTemplateId`).

### Supported templates (matrix)

Regression covers **12** QuestPDF layouts in the John Doe matrix (**01–10**, **49–50**)
plus variant **51** (deferred-sidebar synthetic PDF). Tier **A** (`PdfFull`): variant
**01** (Modern Sidebar stress). Tier **B** (`PdfSidebarCounts`): **02**, **07**, **49**.
Other matrix PDFs use relaxed `PdfTemplateLayout` floors (≥ 20 items, no skill dump).

### Preferred round-trip

**`*.revitae.json`** remains the power-user interchange. PDF re-import targets
**export PDF → edit elsewhere → re-import PDF**.

### Committed stress fixture

`tests/ReVitae.Tests/Import/Fixtures/JohnDoeStressCv.pdf` — regenerate when export
layout or `JohnDoeStressCvDataset` changes:

```bash
dotnet run --project scripts/GenerateJohnDoeStressPdf/GenerateJohnDoeStressPdf.csproj
```

## Regression testing

The **John Doe import matrix** (`tests/ReVitae.Tests/Import/JohnDoeImportRegressionMatrixTests`,
trait `ImportMatrix`) generates **51** fully populated CV files at runtime from
`JohnDoeStressCvDataset` (and synthetic PDF variant **51**) and asserts:

- successful import per variant,
- canonical section counts and spot checks,
- **zero post-import form validation errors** (same validators as the UI).

Filter locally: `dotnet test --filter Category=ImportMatrix`.

## Related docs

- Export formats (including page images): [`export-formats.md`](export-formats.md)
- AI-assisted import fallback: [`ai-import.md`](ai-import.md)
- Native interchange schema: [`revitae-project-json.md`](revitae-project-json.md)
