# Prompt 032 - OCR and Image-Based CV Import

Extend ReVitae import so **scanned PDFs**, **image-only PDF pages**, and **raster
image files** can be imported by adding an **OCR text extraction front-end** that
feeds the **existing deterministic parser**. This prompt intentionally excludes
**AI / LLM parsing** — OCR here means classical optical character recognition
(Tesseract or equivalent), not ChatGPT-style extraction.

Prompt 021 explicitly excluded OCR. This prompt adds that missing path while
**keeping** the current `CvTextImportPipeline` unchanged as the single parsing
brain.

Builds on prompts **017** (intro import UX), **021** (multi-format import), and the
existing **`CvImportDiagnosticsLogger`** (import debug log).

## Goal

Users can import CVs when there is **no reliable text layer**:

1. scanned PDF (photo of paper CV),
2. PDF exported as flat images,
3. photo or scan saved as `.jpg`, `.png`, `.webp`, `.tiff`, etc.

After OCR, the user gets the same experience as text-based import:

- structured form populated as a **draft**,
- confidence hints,
- section collapse rules,
- replace confirmation,
- import debug log (when `REVITAE_IMPORT_DEBUG` is enabled).

## How OCR and the Parser Work Together (Architecture)

**Do not build two parsers and merge `CvImportResult` objects.** That creates
conflicts (duplicate work entries, mismatched names, unclear precedence).

Use a **single pipeline with a pluggable text acquisition step**:

```text
File (PDF / image)
        │
        ▼
┌───────────────────────────┐
│  Text acquisition layer   │  ← NEW in this prompt
│  (ICvTextExtractor impl)  │
└───────────────────────────┘
        │
        │  CvTextExtractionResult (text + warnings + strategy metadata)
        ▼
┌───────────────────────────┐
│  CvTextImportFlows        │  ← EXISTING entry (unchanged signature)
│  CvTextImportPipeline     │
│  Normalize → Segment →    │
│  Extract fields           │
└───────────────────────────┘
        │
        ▼
   CvImportResult
```

### Text acquisition strategy (fallback chain)

**v1 scope (this prompt):** file-level fallback for PDF; direct OCR for raster
images. **Per-page hybrid** (PdfPig on some pages, OCR on others) is documented
below but **deferred to a follow-up** unless trivial to ship with the same PR.

For **PDF**:

1. **Primary:** existing `PdfPigTextExtractor` via `PdfTextExtractorAdapter` —
   fast, preserves text layer and hyperlinks when present.
2. **Quality gate:** if extracted text is empty or below a minimum useful
   threshold, treat PDF as **image-only** and fall back to OCR.
3. **Fallback:** render each page to bitmap and run **OCR per page**; join page
   texts with `\n\n` (same separator as multi-page PdfPig today).
4. **Password-protected PDF:** if PdfPig throws / reports encryption, return
   `ImportErrorPasswordProtected` — **do not** attempt OCR fallback.
5. **Hybrid (follow-up / v1.1):** per page, if page has `< N` words from PdfPig,
   OCR only that page; otherwise keep PdfPig text for that page. Concatenate in
   page order. Never OCR a page that already has usable text — avoids duplicates.

For **image files** (`.jpg`, `.png`, …):

1. OCR the image directly → text.
2. No PdfPig step.

For **ReVitae text PDFs** (sidebar templates, multi-page):

- PdfPig path remains default whenever the quality gate passes.
- Template-aware PdfPig fixes (sidebar defer, column split) stay in
  `PdfPigTextExtractor` — OCR does **not** replace or improve them.
- **Important limitation:** OCR on a **rendered page bitmap** loses column
  structure. Sidebar + main content on a scan will OCR as one messy reading order.
  OCR is for **external** scans, not a substitute for PdfPig on ReVitae exports.

For **Apple `.pages`** (existing `PagesTextExtractor`):

- Keep current behavior: try embedded preview PDF/text first.
- Only if that path yields no usable text should OCR on extracted preview be
  considered (optional enhancement — do not regress existing Pages tests).

### What is NOT merged

| Approach | Verdict |
| -------- | ------- |
| PdfPig text + OCR text → concatenate blindly (same pages) | **Reject** — duplicates content |
| PdfPig result + OCR result → merge two `CvImportResult` | **Reject** — field conflicts |
| PdfPig **or** OCR → one text string → one parser | **Correct** |
| Per-page: text layer **or** OCR (not both) → join pages | **Correct** for hybrid PDFs (v1.1) |

### Hyperlinks and contact fields

OCR generally **does not** recover clickable hyperlinks. After OCR:

- email/phone regex on OCR text still applies,
- LinkedIn/GitHub URLs may be broken across lines (same layout problem as PdfPig
  sidebar PDFs),
- add `ImportWarningOcrUsed` plus existing uncertain-field warnings.

## Relationship to Other Import Paths

| Source | Text acquisition | Parser |
| ------ | ---------------- | ------ |
| `.revitae.json`, JSON Resume, Europass | structured mapper | direct — **no OCR, no text pipeline** |
| `.docx`, `.html`, `.md`, text PDF | format `ICvTextExtractor` | `CvTextImportPipeline` |
| scanned PDF / raster images | **OCR extractor** | `CvTextImportPipeline` |

**Round-trip fidelity:** users who need perfect re-import should use
`*.revitae.json`. OCR import is for **external** scanned/photo CVs.

## Supported Formats (New in This Prompt)

| Format | Extensions | Pipeline |
| ------ | ---------- | -------- |
| Scanned / image-only PDF | `.pdf` | PdfPig → quality gate → OCR fallback |
| JPEG | `.jpg`, `.jpeg` | OCR → text pipeline |
| PNG | `.png` | OCR → text pipeline |
| WebP | `.webp` | OCR → text pipeline (via ImageSharp decode) |
| TIFF | `.tif`, `.tiff` | OCR → text pipeline |
| BMP | `.bmp` | OCR → text pipeline |

### Still out of scope

- `.gif` (animated / limited CV use),
- `.heic` / `.heif` (same deferral as profile photo in prompt 023),
- OCR inside `.docx` embedded images,
- video, email attachments,
- **AI / LLM** structure extraction (Phase 2 / separate prompt),
- cloud OCR APIs as **required** dependency (optional `IOcrEngine` impl allowed),
- automatic OCR language detection beyond configured Tesseract language packs,
- handwriting-optimized models (best effort only),
- real-time camera capture.

## Product Behavior

### File picker

Extend `CvImportFilePickerOptions` and `CvImportFormatDetector`:

- add raster extensions → new `CvImportFormat.RasterImage` enum value (do not
  overload `PlainText` or `Pdf`),
- PDF detection unchanged — PdfPig vs OCR decided inside PDF extractor,
- existing document types unchanged.

User-facing copy must not say “PDF only”. Use **Import existing CV** / **Upload
CV** wording from prompt 021.

### Progress UI

During import, show localized status when OCR runs (slower than PdfPig):

- keep `IntroReadingPdf` while attempting PdfPig on PDF,
- new `ImportRunningOcr` while OCR runs (PDF fallback or raster image),
- do not expose engine name (Tesseract) in UI; log it in import debug file only.

Long-running OCR should respect app/window lifetime (avoid orphaned work after
modal close — cancel token or best-effort abort).

### Warnings

When OCR path is used, prepend warning:

- `ImportWarningOcrUsed` — import relied on OCR; review all fields carefully.

Reuse existing parser warnings (`ImportWarningNameUncertain`, etc.) as today.

When PdfPig quality gate **passes**, do **not** emit `ImportWarningOcrUsed` even
if OCR engine is installed.

### Import debug log

Extend `CvImportDiagnosticsLogger.LogExtraction` to read strategy metadata from
`CvTextExtractionResult` (see Implementation):

```text
--- 1. Text extraction ---
Strategy: PdfTextLayer | Ocr | HybridPdf
OcrEngine: Tesseract 5.x (debug only)
OcrLanguages: eng+slk
Pages: N
…
```

Log full extracted text (PdfPig or OCR) in the debug file only — same as today.

## Core Implementation Plan

### 1. Extend existing types (prefer over parallel models)

Add to `ReVitae.Core/Import/Extraction/`:

```csharp
public enum CvTextAcquisitionStrategy
{
    PdfTextLayer,
    Ocr,
    HybridPdf
}
```

**Extend** `CvTextExtractionResult` (do not introduce a competing record type):

```csharp
public sealed record CvTextExtractionResult(
    bool Success,
    string Text,
    string? ErrorMessageKey,
    IReadOnlyList<string>? HyperlinkUrls = null,
    IReadOnlyList<CvImportWarning>? Warnings = null,
    int? PageCount = null,
    CvTextAcquisitionStrategy? Strategy = null);
```

All existing call sites remain valid via default `Strategy = null` (treat as
legacy / non-OCR extractors).

Register new keys in `TranslationKeys.cs` and `AppLocalizer` like existing import
keys.

### 2. PDF orchestrator as `ICvTextExtractor`

Implement `CompositePdfTextExtractor : ICvTextExtractor` (name illustrative):

```csharp
// Pseudocode
public CvTextExtractionResult Extract(string filePath)
{
    var pig = _pdfPig.Extract(filePath);
    if (pig succeeded && CvTextQualityGate.IsUsable(pig.Text, pig.PageCount))
        return ToExtractionResult(pig, Strategy: PdfTextLayer);

    if (pig is password protected)
        return pig; // fail fast — no OCR

    var ocr = _ocrPdfExtractor.Extract(filePath);
    return ocr with { Strategy = CvTextAcquisitionStrategy.Ocr };
}
```

Wire into `PdfCvFormatImporter` default constructor instead of raw
`PdfPigTextExtractor` only:

```csharp
new PdfCvFormatImporter(new CompositePdfTextExtractor(...))
```

`CvTextImportFlows.FromExtractor` and `CvTextImportPipeline` stay **unchanged**.

### 3. Raster image extractor

`RasterImageCvFormatImporter` + `OcrImageTextExtractor : ICvTextExtractor`:

- decode with **ImageSharp** (already a project dependency),
- run `IOcrEngine`,
- return `CvTextExtractionResult` with `Strategy = Ocr` and
  `ImportWarningOcrUsed` in `Warnings`.

Register `[CvImportFormat.RasterImage] = new RasterImageCvFormatImporter()` in
`CvFormatImporterRegistry`.

### 4. OCR engine adapter

```csharp
public interface IOcrEngine
{
    string Recognize(Image image, OcrOptions options);
}
```

Use `SixLabors.ImageSharp.Image` — not `System.Drawing` (cross-platform parity with
profile photo handling).

Default implementation: **Tesseract** via vetted NuGet binding — verify **macOS /
Windows / Linux** story before merge:

- document whether app bundles `tesseract` + `tessdata` or requires system install,
- CI: default unit tests use **fake `IOcrEngine`**; real Tesseract tests behind
  `[Trait("Category", "OcrIntegration")]` and skipped when binary missing.

Requirements:

- run **locally** (offline), no API key in default path,
- configurable languages (`eng`, `slk`, `ces` — align with app locales over time),
- 300 DPI default for PDF→bitmap render,
- timeout per page,
- surface `ImportErrorOcrUnavailable` when engine binary/data missing.

### 5. PDF → image rendering

PdfPig reads text but does not render. Evaluate (license + native deps):

- `PDFtoImage`, `Docnet.Core`, or Skia-based renderer.

Render constraints:

- max pixel dimension per page (memory cap),
- max page count aligned with practical OCR limits (document constant; may be
  stricter than `CvImportLimits.MaxFileBytes`),
- dispose bitmaps after each page OCR.

### 6. Text quality gate

`CvTextQualityGate.IsUsable(string? text, int? pageCount)` in Core:

| Check | Fail gate |
| ----- | --------- |
| null / whitespace only | yes |
| total non-whitespace chars `< 40` | yes |
| multi-page PDF AND average `< 8` non-whitespace chars per page | yes |
| text contains obvious PdfPig placeholder garbage only (optional v2) | yes |

**Regression:** ReVitae sidebar PDF fixtures and `John Doe.pdf` must **pass**
gate on PdfPig text — add explicit tests. Do not OCR when gate passes.

When gate fails and OCR returns empty → `ImportErrorEmptyDocument` (or new
`ImportErrorOcrFailed` mapped in UI to readable copy).

### 7. Error key mapping

| Situation | Key |
| --------- | --- |
| OCR engine not installed | `ImportErrorOcrUnavailable` |
| OCR ran but no text | `ImportErrorOcrFailed` or reuse `ImportErrorEmptyDocument` |
| Encrypted PDF | existing `ImportErrorPasswordProtected` |
| Unsupported extension | existing `ImportErrorUnsupportedFormat` |

Normalize in `CvDocumentImporter.NormalizeKey` if PDF-specific keys leak from OCR
path.

## Localization

Add to `TranslationKeys.cs` and all supported UI locales:

| Key | English example |
| --- | ---------------- |
| `ImportRunningOcr` | Recognizing text from image… |
| `ImportWarningOcrUsed` | This file was read using OCR. Please review all fields carefully. |
| `ImportErrorOcrUnavailable` | OCR is not available on this system. |
| `ImportErrorOcrFailed` | Could not recognize text in this image or scan. |
| `ImportRasterImageFileType` | Images (JPEG, PNG, …) |

Update `docs/import-formats.md`:

- document OCR fallback for PDF and raster image row in format matrix,
- replace absolute “image-only PDFs not supported” with gated fallback wording,
- note column-layout limitations for OCR.

## Testing

### Unit tests

- `CvTextQualityGateTests` — empty, short, multi-page sparse, ReVitae PDF sample
  passes,
- `CompositePdfTextExtractorTests` with mocked PdfPig + mocked OCR — verify OCR
  not called when PdfPig passes gate,
- `OcrImageTextExtractorTests` with fake `IOcrEngine` returning fixture CV text →
  assert `CvTextImportPipeline` output,
- encrypted PDF mock → no OCR invocation,
- `RasterImageCvFormatImporter` registered in detector for `.png`.

### Integration (optional CI)

- `[Trait("Category", "OcrIntegration")]` — real Tesseract on synthetic PNG,
- skip when `tesseract` not on `PATH`.

### Regression

- **all existing import tests** pass without Tesseract installed,
- `ReVitaeExportedSidebarCv.pdf` still uses PdfPig only (no `ImportWarningOcrUsed`),
- `REVITAE_IMPORT_DEBUG=0` still disables log append,
- `PagesTextExtractor` behavior preserved.

## Dependencies and Licensing

Evaluate and document in README:

| Candidate | Role | Notes |
| --------- | ---- | ----- |
| Tesseract + .NET binding | OCR engine | Native binary + `tessdata` |
| PDF→image library | scanned PDF | Required for PDF OCR fallback |
| ImageSharp | decode raster / page bitmaps | already in repo |

**Do not** require cloud OCR. Optional `IOcrEngine` cloud impl may come later.

## Security and Limits

- same **25 MiB** file size limit as prompt 021 (`CvImportLimits.MaxFileBytes`),
- additional **rendered pixel budget** per document (prevent OOM on huge scans),
- no network in default OCR path,
- debug log may contain full OCR text — local `%LocalAppData%` only; same privacy
  note as current import debug log.

## Tessdata Bundling and First-Run Setup

Default path must work **offline** without requiring users to install Tesseract
manually when possible.

1. **`TessdataLocator`** searches in order:
   - `{AppContext.BaseDirectory}/tessdata/`
   - `%LocalAppData%/ReVitae/tessdata/`
   - `TESSDATA_PREFIX` environment variable
2. **`TessdataBootstrapper`** (first run or when folder empty):
   - copy bundled `eng.traineddata` from app resources into
     `%LocalAppData%/ReVitae/tessdata/` if writable,
   - log copy outcome in import debug file only.
3. Document in README: optional `slk`, `ces` packs for SK/CZ locales; app defaults
   to `eng` until additional packs are bundled.
4. When no traineddata is found → `ImportErrorOcrUnavailable` (no silent failure).

## OCR Post-Processing (Layout from Bounding Boxes)

Tesseract can return **line bounding boxes**. Use them in v1 for basic reading
order — not full column reconstruction:

1. **`OcrLayoutNormalizer`** — sort lines by `(Top, Left)` with a small vertical
   tolerance band (~1.5× median line height) so same-row fragments stay ordered.
2. Join normalized lines with `\n`; preserve paragraph breaks when vertical gap
   exceeds threshold.
3. Log “layout normalized: N lines” in import debug when boxes were used.
4. **Limitation:** two-column scans remain one stream — document in UI warning
   copy; ReVitae sidebar PDFs must stay on PdfPig (prompt 033).

## Force OCR (Advanced Import)

When PdfPig passes the quality gate but layout is wrong (garbled columns, wrong
section assignment on a **text** PDF):

1. Add **`CvImportSessionOptions.ForceOcr`** (thread/async-local for import
   session) read by `CompositePdfTextExtractor`.
2. When `ForceOcr=true`, skip PdfPig and run OCR path directly (still reject
   encrypted PDFs).
3. **UI (v1.1 acceptable):** secondary action on import error retry or hidden
   developer menu — label `ImportForceOcr` (“Import as scan (OCR)”). Not required
   for initial merge if core flag + tests land first.
4. Never auto-force OCR when gate passes — user opt-in only.

## Post-Import OCR Banner

When `ImportWarningOcrUsed` is present in `CvImportResult.Warnings`:

1. After successful import, set **`ExportStatusTextBlock`** (or dedicated import
   status area) to localized `ImportWarningOcrUsed` text — visible until next
   export or form reset.
2. Do not block editing; banner is informational.
3. Intro/replace modals close as today; banner appears on main window.

## ReVitae PDF Re-Import (Separate Prompt)

Template-aware PdfPig fixes for ReVitae-exported sidebar PDFs are **not** part of
this prompt. See **`prompts/033-revitae-pdf-reimport.md`** — higher priority than
OCR for own exports.

## Out of Scope (This Prompt)

- AI / LLM field extraction or merging PdfPig + OCR **results**,
- template-aware PdfPig sidebar parsing improvements (**prompt 033**),
- profile photo extraction from PDF or OCR,
- batch folder import,
- “Export as `.revitae.json` from OCR” wizard,
- handwriting CVs,
- per-page hybrid PDF (unless explicitly pulled into v1 — default defer).

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` and `./scripts/lint-cs.sh` pass,
- `npm run lint` passes if docs touched,
- full test suite passes **without** Tesseract installed,
- manual: scanned PDF or photo with printed email/name → draft import,
- manual: ReVitae text PDF → PdfPig path, no OCR warning,
- README + `docs/import-formats.md` updated.

## Summary for Implementers

**OCR is not a second parser.** It is an alternate `ICvTextExtractor` that
produces **plain text** for the existing `CvTextImportPipeline`. For PDF, try
PdfPig first; OCR only when the quality gate fails. Never merge two
`CvImportResult` objects.
