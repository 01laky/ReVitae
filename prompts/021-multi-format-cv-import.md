# Prompt 021 - Multi-Format CV Import

Extend ReVitae import so users can upload an existing CV in many common document and
structured data formats — not only PDF. All new formats must reuse the existing
structured form, confidence hints, section expand/collapse rules, replace-confirmation
flow, and direct-apply policy from prompt 017.

This prompt intentionally excludes **image-based import and OCR**. Scanned PDFs,
photos, and raster image files remain unsupported.

## Goal

Support the full set of practical non-OCR CV import formats while keeping one
consistent user experience:

1. User picks **Create new CV** or **Import existing CV** on startup, or uses
   **Upload another CV** in the header later.
2. User selects a supported file.
3. ReVitae extracts or deserializes content locally.
4. ReVitae populates the structured form immediately.
5. User reviews and edits imported content in the normal sections.

Import quality expectations remain the same as prompt 017: produce a **useful first
draft**, not perfect structure recovery. Parsing stays **deterministic and
heuristic-based** — no AI in this prompt.

## Current State (Before This Prompt)

Already implemented from prompts 017–020:

- startup intro modal with **Create new CV** and **Import from PDF**,
- header **Upload another CV** action with replace confirmation when form is not
  empty,
- `CvPdfImporter` + PdfPig text extraction,
- shared text pipeline:
  `CvTextNormalizer` → `CvSectionSegmenter` → `CvImportFieldExtractor`,
- `CvImportResult` with warnings, confidence, and `SectionHasData`,
- direct apply via `ApplyCvImportResult(...)`,
- low-confidence import styling,
- localized import errors for PDF-only flows.

Current limitations to remove:

- file picker accepts only `*.pdf`,
- UI copy says PDF everywhere (`Import from PDF`, replace confirm mentions PDF),
- `CvPdfImporter` is format-specific instead of a unified entry point,
- error keys/messages are PDF-specific even when the failure is generic.

## Supported Import Formats

All formats below are in scope. Image/OCR formats are explicitly out of scope
(see **Out of Scope**).

### Category A — Document formats (text extraction → shared parser)

| Format                         | Extensions         | Support level | Notes                                                                                               |
| ------------------------------ | ------------------ | ------------- | --------------------------------------------------------------------------------------------------- |
| PDF                            | `.pdf`             | Full          | Existing PdfPig path; text-based PDFs only                                                          |
| Microsoft Word (Open XML)      | `.docx`            | Full          | Primary editable Word format                                                                        |
| Microsoft Word (legacy binary) | `.doc`             | Best effort   | Older exports; partial extraction acceptable                                                        |
| OpenDocument Text              | `.odt`             | Full          | LibreOffice / OpenOffice                                                                            |
| Rich Text Format               | `.rtf`             | Full          | Common legacy export                                                                                |
| Plain text                     | `.txt`             | Full          | Raw CV text                                                                                         |
| Markdown                       | `.md`, `.markdown` | Full          | Developer / GitHub-style CVs                                                                        |
| HTML                           | `.html`, `.htm`    | Full          | Exported web CVs                                                                                    |
| LaTeX source                   | `.tex`             | Best effort   | Strip markup/commands; parse resulting text                                                         |
| AbiWord                        | `.abw`             | Best effort   | Gzipped XML document                                                                                |
| WPS Office                     | `.wps`             | Limited       | Only if a reliable .NET extractor exists; otherwise explicit unsupported error with export guidance |
| Apple Pages                    | `.pages`           | Limited       | Try embedded preview PDF/text only; otherwise unsupported error                                     |

### Category B — Structured data formats (direct mapping → `CvImportResult`)

| Format                                 | Extensions               | Support level | Notes                                                    |
| -------------------------------------- | ------------------------ | ------------- | -------------------------------------------------------- |
| JSON Resume                            | `.json`                  | Full          | Detect schema by `$schema` or canonical top-level keys   |
| ReVitae project JSON                   | `.json`, `.revitae.json` | Full          | Native structured export/import schema defined below     |
| Europass CV XML                        | `.xml`                   | Full          | EU Europass export                                       |
| HR Open Standards / HR-XML-like CV XML | `.xml`                   | Best effort   | Map common resume nodes when schema matches              |
| YAML CV                                | `.yaml`, `.yml`          | Full          | Same logical schema as JSON Resume or ReVitae JSON       |
| CSV / TSV                              | `.csv`, `.tsv`           | Limited       | Only when file matches supported tabular CV export shape |

### Category C — Explicitly excluded

Do **not** implement import for:

- `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.bmp`, `.tiff`, `.tif`, `.heic`,
- image-only PDFs (no extractable text layer),
- OCR pipelines of any kind,
- email containers (`.eml`, `.msg`) — CV may live inside, but mailbox parsing is
  out of scope,
- generic ZIP archives unless the inner file type is itself supported.

## Product Behavior Summary

### Startup and replace flows

Keep the existing intro and replace flows from prompt 017. Only broaden them:

| UI element                   | Change                                                                       |
| ---------------------------- | ---------------------------------------------------------------------------- |
| Intro import action label    | `Import existing CV` (not PDF-specific)                                      |
| Intro helper text            | Mention supported formats briefly; state image/scanned files are unsupported |
| Header upload button tooltip | `Upload another CV` (keep)                                                   |
| Replace confirm message      | `Importing a new file will replace all current CV data…`                     |
| File picker title            | `Import CV` / `Upload CV`                                                    |
| Progress text                | Format-aware where practical (`Reading document…`, `Parsing CV…`)            |

Behavior otherwise unchanged:

- direct apply after successful import,
- no import preview/diff screen,
- replace confirmation when form already has data,
- collapse empty sections after import,
- low-confidence field styling,
- cancel file picker without error.

### File picker UX

Use one combined file picker filter group named something like **Supported CV
files** with all extensions listed below, plus optional grouped sub-filters only if
the platform file picker supports multiple types cleanly.

Suggested combined pattern list:

```text
*.pdf;*.docx;*.doc;*.odt;*.rtf;*.txt;*.md;*.markdown;*.html;*.htm;*.tex;*.abw;*.wps;*.pages;*.json;*.revitae.json;*.xml;*.yaml;*.yml;*.csv;*.tsv
```

Requirements:

- user can pick any supported extension in one dialog,
- unsupported extension chosen manually (if OS allows) returns localized
  `import.error.unsupportedFormat`,
- do not require the user to know which parser will run.

On platforms where a single combined extension pattern is unreliable (notably some
Avalonia/macOS file-picker combinations), provide **multiple `FilePickerFileType`
entries** grouped under the same localized label, for example:
`Documents`, `Structured files`, and `All supported CV files`.

### Format detection

Detect format using this order:

1. Explicit hint from file picker / caller if available.
2. File extension (case-insensitive).
3. Structured sniffing for ambiguous extensions (order matters for `.json` / `.xml`):
   - `.json` → **first** ReVitae JSON if `"revitaeVersion"` is present, **else** JSON
     Resume if `"basics"`, `"work"`, `"education"`, `"skills"`, or a JSON Resume
     `$schema` is present, **else** unsupported structured JSON error,
   - `.xml` → **first** Europass if Europass namespace/root detected, **else** HR-XML
     heuristics, **else** unsupported XML error,
   - `.yaml`/`.yml` → parse and apply same schema detection as JSON,
   - `.pages` → package handler,
   - otherwise route by extension.

Do not rely on MIME types alone.

## Architecture

Refactor import around a unified facade while preserving the existing text parser.

### High-level pipeline

```text
File path
  → CvImportFormatDetector
  → ICvFormatImporter (per format/category)
      A) text extraction → CvTextImportPipeline (existing parser)
      B) structured mapping → CvStructuredImportMapper
  → CvImportResult
  → MainWindow.ApplyCvImportResult(...)
```

### New / renamed core types

Suggested files:

```text
src/ReVitae.Core/Import/
  CvDocumentImporter.cs                 unified public facade
  CvImportFormat.cs                     enum / registry of supported formats
  CvImportFormatDetector.cs
  CvImportLimits.cs                     max file size (25 MB)
  CvTextImportPipeline.cs               extracted from CvPdfImporter.ImportFromText
  CvImportTextSource.cs                 text + optional hyperlink URLs + warnings
  ICvFormatImporter.cs
  Importers/
    PdfCvFormatImporter.cs
    DocxCvFormatImporter.cs
    DocCvFormatImporter.cs
    OdtCvFormatImporter.cs
    RtfCvFormatImporter.cs
    PlainTextCvFormatImporter.cs
    MarkdownCvFormatImporter.cs
    HtmlCvFormatImporter.cs
    LatexCvFormatImporter.cs
    AbwCvFormatImporter.cs
    WpsCvFormatImporter.cs
    PagesCvFormatImporter.cs
    JsonResumeCvFormatImporter.cs
    ReVitaeJsonCvFormatImporter.cs
    EuropassXmlCvFormatImporter.cs
    HrXmlCvFormatImporter.cs
    YamlCvFormatImporter.cs
    TabularCvFormatImporter.cs
  Extraction/
    ICvTextExtractor.cs                 shared interface for all text-based formats
    CvTextExtractionResult.cs           replaces PdfTextExtractionResult over time
    Pdf/
      IPdfTextExtractor.cs              keep or migrate to ICvTextExtractor
      PdfPigTextExtractor.cs
    ... other format-specific extractors ...
  Structured/
    JsonResumeMapper.cs
    ReVitaeJsonMapper.cs
    EuropassXmlMapper.cs
    HrXmlMapper.cs
    TabularCvMapper.cs
    CvStructuredImportMapper.cs         shared helpers / confidence emission
  Xml/
    SecureXmlReaderFactory.cs             XXE-safe XML reader settings
```

### Public facade

Replace direct UI usage of `CvPdfImporter` with `CvDocumentImporter`.

Suggested API:

```csharp
public sealed class CvDocumentImporter
{
    public CvImportResult ImportFromFile(string filePath);
    public CvImportFormat DetectFormat(string filePath);
}
```

Keep `CvPdfImporter` temporarily as an internal adapter or thin wrapper if useful for
tests, but UI and new tests should call `CvDocumentImporter`.

### Import guards (file size)

Before any extractor or mapper runs, `CvDocumentImporter` must validate the selected
file on disk.

Requirements:

- reject files larger than **25 MB** (26,214,400 bytes),
- use `FileInfo.Length` or equivalent; do not read the whole file just to measure size,
- return `Success = false` with error key `import.error.fileTooLarge`,
- perform this check for **all** import formats, including structured JSON/XML/YAML/CSV,
- if the file does not exist, keep existing file-not-found behavior instead.

Suggested constant location:

- `src/ReVitae.Core/Import/CvImportLimits.cs`

```csharp
public static class CvImportLimits
{
    public const long MaxFileBytes = 25L * 1024 * 1024; // 25 MB
}
```

Do not add import timeouts in this prompt.

Extract the existing method body of `CvPdfImporter.ImportFromText(...)` into
`CvTextImportPipeline.Import(string rawText, IReadOnlyList<string>? hyperlinkUrls)`.

Migration notes:

- `CvPdfImporter` may remain as a thin wrapper over `PdfCvFormatImporter` for
  backward-compatible tests during refactor, but new code must not depend on it.
- Update `CvTextImportPipeline` to return `import.error.emptyDocument` for
  whitespace-only text. Existing PDF-only tests that assert
  `ImportErrorEmptyPdf` should be updated to the generic key **or** assert at the
  PDF extractor level when the failure happens before the shared pipeline.
- `PdfPigTextExtractor` may keep PDF-specific empty/unreadable keys internally;
  the unified facade maps them to generic document keys for UI when appropriate.

### Shared text extraction result

Generalize `PdfTextExtractionResult` into a shared shape for all text-based
extractors:

```csharp
public sealed record CvTextExtractionResult(
    bool Success,
    string Text,
    string? ErrorMessageKey,
    IReadOnlyList<string>? HyperlinkUrls = null,
    IReadOnlyList<CvImportWarning>? Warnings = null,
    int? PageCount = null);              // PDF-only metadata; null for other formats
```

Either migrate `PdfTextExtractionResult` to this type or provide an adapter. Do
not maintain two incompatible result models long term.

PDF-specific keys such as `import.error.emptyPdf` may remain for PDF failures, but
add generic keys too:

- `import.error.emptyDocument`
- `import.error.unreadableDocument`
- `import.error.unsupportedFormat`
- `import.error.unsupportedStructuredFormat`
- `import.error.fileTooLarge`
- `import.error.passwordProtected` (reuse existing key; UI string may become
  document-neutral: “Password-protected documents are not supported.”)

Prefer generic keys in the unified facade; format-specific keys are allowed when
they improve user guidance (for example password-protected PDF).

## Part 1 - Text Extraction Implementations

Each Category A importer should:

1. validate file existence,
2. extract plain text locally,
3. optionally collect hyperlink URLs when the source exposes them,
4. pass text into `CvTextImportPipeline`,
5. merge extractor warnings into final `CvImportResult`.

### PDF (existing)

Keep PdfPig implementation from prompt 017.

Requirements:

- preserve hyperlink extraction,
- preserve password-protected and unreadable PDF errors,
- no OCR fallback when text is empty.

### DOCX

Recommended library: **`DocumentFormat.OpenXml`**.

Implementation notes:

- read main document body text in natural reading order,
- preserve paragraph breaks as `\n`,
- extract hyperlink targets from relationships when available,
- ignore headers/footers in v1 unless easy to include,
- tables: flatten cells row-by-row with tab or newline separators,
- unsupported features (text boxes, complex shapes) may be skipped with warning
  `import.warning.partialDocumentContent`,
- password-protected or encrypted DOCX files must return
  `import.error.passwordProtected` when detected, otherwise
  `import.error.unreadableDocument`.

### DOC (legacy Word)

Recommended library: **`NPOI`** (`NPOI.HWPF`).

Best-effort requirements:

- extract readable paragraph text,
- if library cannot open file, return `import.error.unreadableDocument`,
- do not crash on unsupported binary variants,
- add tests with at least one small generated/saved `.doc` fixture if feasible; if
  not feasible to commit a binary fixture, document why and cover through an
  abstraction/mock test plus manual QA note.

### ODT

Recommended approach: treat ODT as ZIP + parse `content.xml`.

Implementation notes:

- use `System.IO.Compression` or an existing ZIP helper,
- parse `content.xml` with `SecureXmlReaderFactory` — ODT is still untrusted input,
- parse XML with secure reader settings,
- extract text nodes from `text:p`, `text:h`, and table cells,
- preserve line breaks between paragraphs,
- ignore styles/images in v1.

No external native dependency required.

### RTF

Recommended library: **`RtfPipe`**.

Requirements:

- convert RTF to plain text,
- preserve paragraph breaks,
- strip control words that do not contribute readable content.

### Plain text

Read file using UTF-8 first.

Fallback encoding detection:

1. UTF-8 with BOM,
2. UTF-16 LE/BE if BOM present,
3. Windows-1250 / ISO-8859-2 only if needed for common Central European exports,
   otherwise UTF-8 and replace invalid sequences safely.

Do not silently mangle Slovak/Czech diacritics.

### Markdown

Recommended library: **`Markdig`**.

Requirements:

- convert Markdown to plain text for the shared parser using Markdig,
- prefer a plain-text-oriented Markdig renderer or HTML renderer followed by the
  same HTML-to-text stripping rules as the HTML extractor — do not execute raw HTML
  blocks,
- preserve bullets and headings as readable lines,
- ignore embedded images (no OCR),
- raw HTML blocks inside Markdown should be stripped to text, not rendered in a
  browser engine.

### HTML

Recommended library: **`HtmlAgilityPack`**.

Requirements:

- remove `script`, `style`, `noscript`, and hidden nodes,
- prefer `innerText` semantics over naive tag stripping,
- preserve block-level breaks between headings, paragraphs, list items, and table
  rows,
- extract `href` values from `<a>` tags into hyperlink list for link parsing,
- do not fetch remote URLs.

### LaTeX

No full TeX engine in v1.

Requirements:

- read `.tex` source as text,
- remove comments (`% ...`),
- strip common formatting commands while keeping their arguments as plain text,
- convert `\section{Work Experience}` style section commands into standalone lines
  that the section segmenter can detect,
- unsupported macros should be ignored, not executed,
- add warning `import.warning.latexPartiallyNormalized`.

### AbiWord (`.abw`)

Requirements:

- detect gzip wrapper,
- decompress and parse AbiWord XML using `SecureXmlReaderFactory`,
- extract text from `<p>`/paragraph nodes,
- best-effort only with clear unreadable error on failure.

### WPS (`.wps`)

Requirements:

- attempt extraction via chosen .NET-compatible library if one is added,
- if no reliable extractor exists, return `import.error.unsupportedFormat` with
  localized guidance to export as DOCX/PDF/RTF,
- do not add LibreOffice headless or other external binary dependencies in this
  prompt.

Document the chosen approach in code comments only where non-obvious.

### Apple Pages (`.pages`)

Limited support only.

Requirements:

- treat `.pages` as a package/ZIP when possible,
- first try to locate an embedded `preview.pdf` or `Preview.pdf` and parse it with
  the existing PDF extractor,
- if no embedded preview exists, fail with `import.error.unsupportedFormat` and
  localized message suggesting export to PDF or DOCX from Pages,
- do not attempt OCR on preview images,
- do not bundle Apple proprietary parsers.

## Part 2 - Structured Import Implementations

Structured importers map source data directly into existing entry models and
`PersonalInformationImport`. They should still emit:

- `SectionHasData`,
- `FieldConfidences`,
- non-fatal `Warnings` when fields are skipped or partially mapped.

Structured imports may bypass `CvSectionSegmenter`, but should reuse
`CvStructuredImportMapper` helpers for confidence and section flags.

### Secure XML parsing (required)

Any XML parsing in this prompt — Europass, HR-XML, ODT `content.xml`, AbiWord XML,
and future import XML paths — must be **XXE-safe**.

Add a shared helper, for example:

- `src/ReVitae.Core/Import/Xml/SecureXmlReaderFactory.cs`

Requirements:

- use `XmlReader` / `XmlReaderSettings` with:
  - `DtdProcessing = DtdProcessing.Prohibit`,
  - `XmlResolver = null`,
  - no external entity resolution,
  - no network access during parse,
- do **not** use unsafe defaults such as `XmlDocument.Load(path)` without secure
  settings,
- `System.Xml.Linq` (`XDocument.Load`) must use the same secure `XmlReader` via
  `XmlReader.Create(stream, settings)`,
- malformed XML → `import.error.unreadableDocument`,
- entity-expansion / DTD attempts must fail safely without crashing the app.

Add unit tests that prove external entity payloads are not resolved (see Part 5).

### JSON Resume

Schema reference: [JSON Resume schema](https://jsonresume.org/schema/).

Detection rules:

- top-level object with `basics`, `work`, `education`, `skills`, or `$schema` pointing
  to JSON Resume.

Map at minimum:

| JSON Resume                                 | ReVitae                                         |
| ------------------------------------------- | ----------------------------------------------- |
| `basics.name`                               | split into first/last name heuristically        |
| `basics.label`                              | professional title                              |
| `basics.email`                              | email                                           |
| `basics.phone`                              | phone                                           |
| `basics.url` / `basics.profiles[]`          | portfolio / LinkedIn / GitHub / links           |
| `basics.summary`                            | short summary                                   |
| `basics.location.city/countryCode`          | location                                        |
| `work[]`                                    | work experience entries                         |
| `education[]`                               | education entries                               |
| `skills[]`                                  | skills groups/items                             |
| `languages[]`                               | languages                                       |
| `certificates[]`                            | certificates                                    |
| `projects[]`                                | projects                                        |
| `volunteer[]`, `awards[]`, `publications[]` | map when practical, else additional information |
| `references`, `meta`, unknown arrays        | additional information or ignored with warning  |

Use `High` confidence for direct field mappings, `Medium` when splitting names or
dates, `Low` when inferring labels/hosts.

### ReVitae project JSON

Define a native schema for future persistence compatibility.

Suggested top-level shape:

```json
{
  "revitaeVersion": 1,
  "personalInformation": {},
  "workExperience": [],
  "education": [],
  "skills": [],
  "languages": [],
  "certificates": [],
  "projects": [],
  "links": [],
  "additionalInformation": { "content": "" }
}
```

Requirements:

- accept `.json` and `.revitae.json`,
- version field required; unsupported future versions return
  `import.error.unsupportedStructuredFormat`,
- map fields directly to existing Core models with minimal transformation,
- use `System.Text.Json` property names aligned with the Core CV models and the
  future save/load prompt (document exact names in `docs/revitae-project-json.md`),
- this schema should align with the future save/load prompt, but persistence itself
  remains out of scope here.

Add schema documentation in `docs/revitae-project-json.md`.

### Europass XML

Requirements:

- parse using `SecureXmlReaderFactory` (see **Secure XML parsing** above),
- detect Europass namespace/root elements,
- map personal info, experience, education, languages, skills, certificates when
  present,
- unmapped Europass sections append to additional information,
- preserve multilingual content,
- add fixture based on a minimal valid Europass sample XML committed to tests.

### HR-XML / HR Open Standards XML

Best-effort only.

Requirements:

- parse using `SecureXmlReaderFactory` (see **Secure XML parsing** above),
- detect common resume/candidate root patterns,
- map obvious personal/contact/experience/education nodes,
- if schema cannot be recognized, fail with `import.error.unsupportedStructuredFormat`,
  not silent garbage import.

### YAML CV

Recommended library: **`YamlDotNet`**.

Requirements:

- parse YAML into the same in-memory shape used for JSON Resume / ReVitae JSON
  detection,
- reuse the same mappers,
- invalid YAML → `import.error.unreadableDocument`.

### CSV / TSV (limited)

Only import tabular files that match a supported shape.

Support v1 shape:

- header row with recognizable column names such as `firstName`, `lastName`,
  `email`, `phone`, `title`, `summary`, `location`,
- single-row CV export.

Requirements:

- delimiter autodetect between comma and tab,
- if multiple rows exist, import the first data row only and add warning
  `import.warning.tabularMultipleRowsIgnored`,
- if headers are unrecognized, fail with `import.error.unsupportedStructuredFormat`,
- do not attempt to import arbitrary spreadsheets.

## Part 3 - UI and MainWindow Changes

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

### Rename PDF-specific UI hooks

Suggested renames:

| Old                         | New                          |
| --------------------------- | ---------------------------- |
| `ImportPdfButton`           | `ImportCvButton`             |
| `ImportPdfButtonTextBlock`  | `ImportCvButtonTextBlock`    |
| `OnImportPdfClicked`        | `OnImportCvClicked`          |
| `ImportCvFromPdfAsync(...)` | `ImportCvFromFileAsync(...)` |

Keep behavior identical aside from broader file support.

### File picker orchestration

Replace PDF-only picker filter with combined supported formats.

Progress UI:

- `Reading document…` while extracting,
- `Parsing CV…` while parsing/mapping,
- allow optional format label in debug/logs only, not required in user-facing text.

Replace flow:

- update confirm/progress/error copy to say **CV file** or **document**, not PDF,
- keep retry behavior.

### Apply path

No changes to direct apply semantics:

- `ApplyCvImportResult(result)` remains the single apply method,
- confidence styling and section expand/collapse unchanged,
- replace confirmation still based on existing `HasCvFormData()` logic in
  `MainWindow.axaml.cs` (do not rename unless needed).

## Part 4 - Internationalization

Add/update keys in:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

**Prefer updating existing intro/import keys** rather than introducing parallel
PDF-only strings. Rename constants only when it improves clarity and update all
references.

| Existing key / constant                                      | Action                                                      | English direction                                                                                                                                  |
| ------------------------------------------------------------ | ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IntroImportPdf` (`intro.importPdf`)                         | update string; optional rename to `IntroImportCv`           | `Import existing CV`                                                                                                                               |
| `IntroSubtitle` (`intro.subtitle`)                           | update string                                               | mention multi-format import, not PDF-only                                                                                                          |
| `IntroHelper` (`intro.helper`)                               | update string                                               | `Works with PDF, Word, OpenDocument, RTF, text, Markdown, HTML, JSON, XML, and other common CV files. Scanned/image-only files are not supported.` |
| `IntroReadingPdf` (`intro.readingPdf`)                       | update string; optional rename to `IntroReadingDocument`    | `Reading document…`                                                                                                                                |
| `IntroParsingCv` (`intro.parsingCv`)                         | keep key, verify string                                     | `Parsing CV…`                                                                                                                                      |
| `ImportPdfFileType` (`import.pdfFileType`)                   | update string; optional rename to `ImportSupportedFileType` | `Supported CV files`                                                                                                                               |
| `UploadCvFilePickerTitle` (`import.uploadCvFilePickerTitle`) | update string                                               | `Upload CV`                                                                                                                                        |
| `ReplaceCvConfirmMessage` (`modal.replaceCvConfirm.message`) | update string                                               | `Importing a new file will replace all current CV data. Do you want to continue?`                                                                  |

Add new generic error/warning keys:

| Key                                         | English direction                                                                                |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `import.error.unsupportedFormat`            | `This file type is not supported. Export the CV as PDF, DOCX, RTF, or plain text and try again.` |
| `import.error.unsupportedStructuredFormat`  | `The file is recognized, but its structure is not supported.`                                    |
| `import.error.emptyDocument`                | `The file does not contain extractable text.`                                                    |
| `import.error.unreadableDocument`           | `The file could not be read.`                                                                    |
| `import.error.fileTooLarge`                 | `This file is too large to import. Maximum size is 25 MB.`                                       |
| `import.error.passwordProtected`            | `Password-protected documents are not supported.`                                                |
| `import.warning.partialDocumentContent`     | `Some document content could not be imported.`                                                   |
| `import.warning.latexPartiallyNormalized`   | `LaTeX markup was simplified before parsing.`                                                    |
| `import.warning.tabularMultipleRowsIgnored` | `Only the first row of the table was imported.`                                                  |

Keep backward-compatible PDF-specific keys (`import.error.emptyPdf`,
`import.error.unreadablePdf`, `import.error.passwordProtected`) if still used
internally by the PDF extractor, but UI and the unified facade should prefer
generic document wording.

Also remove or localize hardcoded fallback strings in `MainWindow.axaml` that still
say `Reading PDF…`, `Import from PDF`, or PDF-only replace text.

Every supported UI language must receive all new/updated keys.

## Part 5 - Unit and Integration Tests

Add tests under `tests/ReVitae.Tests/Import/`.

Testing is a **first-class deliverable** of this prompt. Every format importer,
text extractor, structured mapper, and shared parser stage must ship with
**exhaustive edge-case coverage** — not only happy-path fixtures.

### Testing principles

1. **One concern per test** — prefer many small `[Fact]` / `[Theory]` tests over
   one giant integration test.
2. **Name tests by scenario** — `Extract_ReturnsEmptyDocumentError_WhenDocxContainsOnlyImages`.
3. **Assert outcomes, not implementation** — verify extracted text, mapped models,
   warnings, confidence, and error keys.
4. **Use inline strings for parser edge cases** — fast, readable, no fixture file
   needed for every variant.
5. **Use committed binary/text fixtures for end-to-end format coverage** — at
   least one realistic file per format plus dedicated edge-case fixtures where
   binary structure matters.
6. **Extend existing edge-case suites** — build on `CvImportEdgeCaseTests.cs` and
   `CvPdfImporterEdgeCaseTests`; update assertions when error keys become generic.
7. **No optional edge-case tests** — if a scenario is listed below, it must be
   implemented unless explicitly marked _N/A with comment_.

Suggested structure:

```text
tests/ReVitae.Tests/Import/
  FormatDetectionEdgeCaseTests.cs
  CvDocumentImporterEdgeCaseTests.cs
  CvTextImportPipelineEdgeCaseTests.cs
  CvTextNormalizerEdgeCaseTests.cs          extend existing
  CvSectionSegmenterEdgeCaseTests.cs        extend existing
  CvImportFieldExtractorEdgeCaseTests.cs    extend existing
  DateRangeParserEdgeCaseTests.cs           extend existing
  CvImportPatternsEdgeCaseTests.cs
  TextExtractors/
    PdfTextExtractorEdgeCaseTests.cs        extend existing
    DocxTextExtractorEdgeCaseTests.cs
    DocTextExtractorEdgeCaseTests.cs
    OdtTextExtractorEdgeCaseTests.cs
    RtfTextExtractorEdgeCaseTests.cs
    PlainTextExtractorEdgeCaseTests.cs
    MarkdownTextExtractorEdgeCaseTests.cs
    HtmlTextExtractorEdgeCaseTests.cs
    LatexTextExtractorEdgeCaseTests.cs
    AbwTextExtractorEdgeCaseTests.cs
    WpsTextExtractorEdgeCaseTests.cs
    PagesPackageExtractorEdgeCaseTests.cs
  Structured/
    JsonResumeMapperEdgeCaseTests.cs
    ReVitaeJsonMapperEdgeCaseTests.cs
    EuropassXmlMapperEdgeCaseTests.cs
    HrXmlMapperEdgeCaseTests.cs
    YamlCvImporterEdgeCaseTests.cs
    TabularCvImporterEdgeCaseTests.cs
    CvStructuredImportMapperEdgeCaseTests.cs
    SecureXmlReaderFactoryEdgeCaseTests.cs
  Fixtures/
    Text/
    Docx/
    Odt/
    Rtf/
    Html/
    Markdown/
    Latex/
    Json/
    Xml/
    Yaml/
    Csv/
    Pdf/                     keep existing PDF fixtures
    EdgeCases/               malformed/partial files per format
```

### Minimum fixture coverage

Commit small intentional fixtures for each format category:

| Fixture                     | Purpose                                               |
| --------------------------- | ----------------------------------------------------- |
| `sample-cv-en-basic.docx`   | contact + summary + one work + one education + skills |
| `sample-cv-en-basic.odt`    | same content as DOCX fixture                          |
| `sample-cv-en-basic.rtf`    | same core content                                     |
| `sample-cv-en-basic.html`   | headings + paragraphs + one link                      |
| `sample-cv-en-basic.md`     | markdown headings and bullets                         |
| `sample-cv-en-basic.tex`    | `\section` + basic entries                            |
| `sample-cv-en-basic.txt`    | plain text version                                    |
| `sample-cv-jsonresume.json` | JSON Resume compliant                                 |
| `sample-cv-revitae.json`    | native ReVitae schema                                 |
| `sample-cv-europass.xml`    | minimal Europass sample                               |
| `sample-cv-en-basic.yaml`   | YAML equivalent of JSON Resume sample                 |
| `sample-cv-single-row.csv`  | one-row export                                        |
| existing PDF fixtures       | keep and reuse                                        |

Additional **edge-case fixtures** under `Fixtures/EdgeCases/`:

| Fixture                                | Purpose                                  |
| -------------------------------------- | ---------------------------------------- |
| `empty.docx`, `empty.odt`, `empty.rtf` | zero readable content                    |
| `tables-only.docx`                     | table-flatten extraction                 |
| `hyperlinks.docx`                      | embedded link targets                    |
| `multilang-headers-sk.docx`            | Slovak section headers through DOCX path |
| `invalid.docx` / `corrupt.odt`         | unreadable archive/XML                   |
| `password-protected.pdf`               | encrypted PDF failure                    |
| `image-only.pdf`                       | empty text, no OCR attempt               |
| `ambiguous.json`                       | neither JSON Resume nor ReVitae          |
| `revitae-v999.json`                    | unsupported schema version               |
| `invalid.yaml`                         | malformed YAML                           |
| `unknown-xml.xml`                      | unrecognized XML schema                  |
| `multi-row.csv`                        | tabular multi-row warning                |
| `pages-no-preview.pages` or mock       | unsupported Pages package                |

### Part 5A - Shared infrastructure edge cases

#### `CvImportFormatDetector`

| Scenario                                                  | Expected outcome                       |
| --------------------------------------------------------- | -------------------------------------- |
| Each supported extension maps to correct `CvImportFormat` | detected                               |
| Extension uppercase (`.DOCX`, `.PDF`)                     | detected                               |
| Unknown extension (`.xyz`, `.png`)                        | `unsupportedFormat`                    |
| `.json` with `revitaeVersion`                             | ReVitae JSON                           |
| `.json` with JSON Resume keys                             | JSON Resume                            |
| `.json` with unrelated JSON (`{"foo":1}`)                 | unsupported structured error at import |
| `.xml` Europass root/namespace                            | Europass                               |
| `.xml` HR-XML-like root                                   | HR-XML                                 |
| `.xml` random XML                                         | unsupported structured error           |
| `.yaml` equivalent of JSON Resume                         | YAML → JSON Resume path                |
| Missing file path / empty path                            | file-not-found                         |
| Directory path instead of file                            | unreadable/unsupported                 |

#### `CvDocumentImporter` (facade)

| Scenario                                         | Expected outcome                                   |
| ------------------------------------------------ | -------------------------------------------------- |
| Happy path per format                            | `Success = true`, populated sections               |
| File size 25 MB + 1 byte                         | `import.error.fileTooLarge`, extractor not invoked |
| File size exactly 25 MB                          | allowed (boundary inclusive)                       |
| Missing file                                     | file-not-found                                     |
| Extractor throws unexpectedly                    | caught → unreadable error, no crash                |
| Extractor returns empty text                     | empty document error                               |
| Parser returns no structured data                | `import.error.noStructuredData`                    |
| Partial parse with warnings                      | `Success = true` + warnings preserved              |
| Password-protected PDF/DOCX if detectable        | password-protected error                           |
| Unsupported extension                            | unsupported format error                           |
| Same file imported twice                         | deterministic identical result                     |
| Very long file path / unicode path               | no crash on common OS paths                        |
| Hyperlink URLs merged from extractor into parser | personal/links populated                           |

#### `CvTextImportPipeline`

| Scenario                             | Expected outcome                                         |
| ------------------------------------ | -------------------------------------------------------- |
| Whitespace-only text                 | empty document error                                     |
| Single-line CV (no headers)          | warning `noSectionsDetected`, content in additional info |
| Text with `\r\n`, `\r`, `\n` mixed   | normalized consistently                                  |
| Null/empty hyperlink list            | parser still succeeds                                    |
| Duplicate hyperlink URLs             | deduplicated                                             |
| Extractor warnings + parser warnings | merged in final result                                   |

### Part 5B - Shared text parser edge cases (extend prompt 017 suites)

These suites already exist — **extend them**, do not shrink coverage.

#### `CvTextNormalizer`

| Scenario                                      | Expected outcome                                     |
| --------------------------------------------- | ---------------------------------------------------- |
| Unicode dashes (`–`, `—`, `−`)                | normalized to `-`                                    |
| Mixed bullet glyphs (`•`, `◦`, `▪`, `*`, `-`) | canonical `-` bullet                                 |
| Repeated inline whitespace                    | collapsed safely                                     |
| 3+ blank lines                                | collapsed to 2                                       |
| Trailing spaces per line                      | trimmed                                              |
| Empty string / whitespace only                | empty output                                         |
| Slovak/Czech diacritics preserved             | unchanged meaning                                    |
| Page-number-like repeated footer lines        | stripped when repeated across pages (if implemented) |
| Null input                                    | empty string or throw — document chosen behavior     |

#### `CvSectionSegmenter`

| Scenario                                                                      | Expected outcome                                         |
| ----------------------------------------------------------------------------- | -------------------------------------------------------- |
| English headers for all sections                                              | all section IDs detected                                 |
| Slovak/Czech headers (`Profil`, `Pracovné skúsenosti`, `Vzdelanie`, `Jazyky`) | detected                                                 |
| Header with trailing colon (`Skills:`)                                        | detected                                                 |
| ALL CAPS headers                                                              | detected                                                 |
| `Contact` section                                                             | detected separately                                      |
| Duplicate same section (`Skills` twice)                                       | bodies merged                                            |
| Near-duplicate sections (`Skills` + `Technical Skills`)                       | merged into skills                                       |
| Word `experience` inside sentence                                             | **not** treated as work-experience header                |
| No headers at all                                                             | warning + additional information fallback                |
| Header line that is also an email/URL/date                                    | not treated as header                                    |
| Ambiguous short line                                                          | prefer longer keyword match                              |
| Summary vs personal block before first header                                 | personal parsed from header block                        |
| Multiple work experience entries in one section                               | single body passed downstream                            |
| Empty section body after header                                               | extractor leaves section empty; `SectionHasData = false` |

#### `DateRangeParser`

| Scenario                                                 | Expected outcome                       |
| -------------------------------------------------------- | -------------------------------------- |
| `01/2020 - 03/2024`                                      | full range                             |
| `Jan 2020 - Mar 2024`                                    | month names parsed                     |
| `2020 - 2024`                                            | year-only range                        |
| `2020 - Present` / `2020 - current` / `2020 - súčasnosť` | `IsPresent = true`                     |
| Single date `06/2006`                                    | start only                             |
| En-dash/em-dash separators                               | parsed                                 |
| Invalid month (`13/2020`)                                | false or partial — document behavior   |
| Non-date line                                            | `TryParse` false                       |
| Date embedded in longer sentence                         | false unless dedicated pattern matches |
| Open-ended start with present end                        | `IsCurrentlyWorking` downstream        |

#### `CvImportPatterns` (email, phone, URL, bullets, names)

| Scenario                                             | Expected outcome                        |
| ---------------------------------------------------- | --------------------------------------- |
| Valid/invalid email variants                         | match/no match                          |
| Phone with `+421`, spaces, parentheses               | parsed                                  |
| `linkedin.com/in/...`, `github.com/...`              | host detection                          |
| Multiple URLs — first portfolio vs social precedence | correct assignment                      |
| Relative URLs / malformed URLs                       | skipped safely                          |
| Labeled fields (`Email:`, `Phone:`, `Location:`)     | high-confidence extraction              |
| Name split: two-token, three-token, single-token     | low-confidence warning for single-token |
| Bullet lines with leading whitespace                 | parsed as achievements                  |
| Technology line `Technologies:`, `Tech:`, `Stack:`   | technologies field, not description     |

#### `CvImportFieldExtractor`

Extend with edge cases for **every section**:

**Personal / header block**

| Scenario                                                             | Expected outcome              |
| -------------------------------------------------------------------- | ----------------------------- |
| Name on line 1, title line 2, email line 3                           | all extracted                 |
| Single-token name                                                    | name uncertain warning        |
| Summary section vs inline summary                                    | summary section wins          |
| LinkedIn/GitHub/portfolio from hyperlinks only (no visible URL text) | populated from hyperlink list |
| Duplicate URL in links vs personal                                   | duplicate skipped warning     |
| Location with postal code / multi-line contact                       | best-effort location          |
| Multiple emails                                                      | first strong match            |

**Work experience**

| Scenario                                | Expected outcome                                    |
| --------------------------------------- | --------------------------------------------------- |
| `Title at Company` format               | split title/company                                 |
| `Company - Title` format                | split correctly                                     |
| Location line before date               | location extracted                                  |
| Date line before description            | dates extracted                                     |
| Bullets + technologies line             | achievements + technologies separated               |
| Sidebar skill bleed into job block      | skills not duplicated into description/technologies |
| Multi-paragraph description with commas | not mistaken for technologies                       |
| Multiple jobs in one section            | multiple entries                                    |
| Present/current end date                | `IsCurrentlyWorking = true`                         |
| Job with only one line                  | partial entry + warning if applicable               |

**Education**

| Scenario                                      | Expected outcome           |
| --------------------------------------------- | -------------------------- |
| Degree + institution + date range             | full entry                 |
| Graduation-only date with inferred start      | low-confidence start dates |
| Leading location line                         | location extracted         |
| Degree keywords (`BSc`, `MSc`, `Ing.`, `PhD`) | degree detected            |

**Skills**

| Scenario                                     | Expected outcome    |
| -------------------------------------------- | ------------------- |
| Comma-separated single line                  | default group       |
| Category lines `Programming: C#, Go`         | named groups        |
| Bullet list                                  | default group items |
| Mixed category + bullets                     | multiple groups     |
| Proficiency/years only when explicit pattern | otherwise empty     |

**Languages**

| Scenario                 | Expected outcome                 |
| ------------------------ | -------------------------------- |
| `English — Fluent`       | proficiency mapped               |
| `German - B2`            | CEFR mapped                      |
| Unknown proficiency text | fields left empty, no wrong enum |
| Native / mother tongue   | native proficiency               |

**Certificates / projects / links / additional**

| Scenario                                         | Expected outcome                      |
| ------------------------------------------------ | ------------------------------------- |
| Certificate with issuer + year on separate lines | mapped                                |
| Project with URL + tech line + description       | mapped                                |
| Extra URLs → link entries with inferred labels   | mapped                                |
| Unmapped trailing text                           | appended to additional info + warning |

**Confidence and section flags**

| Scenario                                     | Expected outcome |
| -------------------------------------------- | ---------------- |
| Email → high confidence                      | emitted          |
| Inferred first name → low confidence         | emitted          |
| Empty sections → `SectionHasData = false`    | correct map      |
| Populated sections → `SectionHasData = true` | correct map      |

### Part 5C - Per-format text extractor edge cases

Each extractor gets its own test class. Minimum scenarios listed below are
**required**.

#### PDF (`PdfPigTextExtractor`)

| Scenario                                | Expected outcome                     |
| --------------------------------------- | ------------------------------------ |
| Valid text-based PDF fixture            | non-empty text                       |
| Multi-page PDF                          | pages joined with `\n\n`             |
| Hyperlinks extracted                    | URL list populated                   |
| Missing file                            | file-not-found                       |
| Corrupt bytes                           | unreadable                           |
| Password-protected PDF                  | password-protected error             |
| Password-protected DOCX (if detectable) | password-protected error             |
| Empty/textless PDF (incl. image-only)   | empty PDF/document error, **no OCR** |
| PDF with repeated headers/footers       | text still usable                    |

#### DOCX

| Scenario                                | Expected outcome                 |
| --------------------------------------- | -------------------------------- |
| Basic fixture end-to-end                | key strings present              |
| Empty document                          | empty document error             |
| Paragraphs + line breaks                | preserved                        |
| Table cells                             | flattened with separators        |
| Hyperlinks in relationships             | URLs extracted                   |
| Bold/italic runs                        | plain text content preserved     |
| Header/footer-only doc (if skipped)     | warning partial content or empty |
| Corrupt/invalid zip/docx                | unreadable                       |
| Password-protected DOCX (if detectable) | password-protected error         |
| Very long document                      | no crash, reasonable memory      |

#### DOC (legacy)

| Scenario                        | Expected outcome           |
| ------------------------------- | -------------------------- |
| Readable `.doc` fixture or mock | text extracted             |
| Corrupt `.doc`                  | unreadable, no crash       |
| Empty `.doc`                    | empty document error       |
| Unsupported binary variant      | unreadable with safe error |

#### ODT

| Scenario                       | Expected outcome     |
| ------------------------------ | -------------------- |
| Basic ODT fixture              | content extracted    |
| Invalid zip                    | unreadable           |
| Missing `content.xml`          | unreadable           |
| Empty `content.xml`            | empty document error |
| Headings + paragraphs + tables | flattened text       |
| Slovak diacritics preserved    | yes                  |

#### RTF

| Scenario                 | Expected outcome         |
| ------------------------ | ------------------------ |
| Basic RTF fixture        | text extracted           |
| Empty RTF                | empty document error     |
| Corrupt RTF              | unreadable               |
| Font/color control words | stripped, text preserved |
| Embedded images in RTF   | ignored, no OCR          |

#### Plain text (`.txt`)

| Scenario                                                | Expected outcome     |
| ------------------------------------------------------- | -------------------- |
| UTF-8 content                                           | correct              |
| UTF-8 BOM                                               | handled              |
| UTF-16 LE/BE with BOM                                   | handled              |
| Central European legacy encoding fixture (if supported) | diacritics preserved |
| Empty file                                              | empty document error |
| Single newline file                                     | still parseable      |
| Very long lines                                         | no crash             |

#### Markdown

| Scenario                    | Expected outcome                                   |
| --------------------------- | -------------------------------------------------- |
| Headings `#` / `##`         | become readable section-like lines                 |
| Bullet lists                | preserved                                          |
| Links `[text](url)`         | URL available to parser (inline or hyperlink list) |
| Inline code / bold / italic | plain text remains                                 |
| Raw HTML block in MD        | stripped to text, not executed                     |
| Image syntax `![](img.png)` | ignored, no OCR                                    |
| Empty file                  | empty document error                               |

#### HTML

| Scenario                                              | Expected outcome            |
| ----------------------------------------------------- | --------------------------- |
| Basic HTML fixture                                    | text extracted              |
| `<script>`, `<style>`, `<noscript>` removed           | not in output               |
| Headings, `p`, `li`, `br`                             | line breaks preserved       |
| Table rows                                            | flattened                   |
| `<a href="...">` links                                | hyperlink list populated    |
| Hidden elements (`display:none` class/id if detected) | skipped                     |
| Malformed HTML                                        | best-effort parse, no crash |
| Empty `<body>`                                        | empty document error        |

#### LaTeX

| Scenario                           | Expected outcome             |
| ---------------------------------- | ---------------------------- |
| `\section{Work Experience}`        | section line normalized      |
| Comments `% ...`                   | removed                      |
| `\textbf{name}` / `\emph{text}`    | arguments kept as text       |
| `\begin{itemize}` bullets          | readable bullets             |
| Empty `.tex`                       | empty document error         |
| Unsupported macros                 | ignored safely               |
| Warning `latexPartiallyNormalized` | emitted when markup stripped |

#### AbiWord (`.abw`)

| Scenario                          | Expected outcome     |
| --------------------------------- | -------------------- |
| Valid gzipped XML fixture or mock | text extracted       |
| Non-gzip file                     | unreadable           |
| Invalid XML inside                | unreadable           |
| Empty document                    | empty document error |

#### WPS (`.wps`)

| Scenario                           | Expected outcome                 |
| ---------------------------------- | -------------------------------- |
| Supported extractable fixture/mock | text extracted **or**            |
| Unrecognized binary                | unsupported format with guidance |
| Empty/unreadable                   | safe error, no crash             |

#### Apple Pages (`.pages`)

| Scenario                          | Expected outcome                       |
| --------------------------------- | -------------------------------------- |
| Package with embedded preview PDF | PDF extractor invoked, import succeeds |
| Package without preview           | unsupported format error               |
| Non-package file renamed `.pages` | unreadable/unsupported                 |
| Preview PDF is image-only         | empty document error, no OCR           |

### Part 5D - Structured mapper edge cases

#### JSON Resume

| Scenario                                        | Expected outcome                |
| ----------------------------------------------- | ------------------------------- |
| Full valid fixture                              | all mapped sections             |
| Missing optional sections                       | empty sections, still success   |
| `basics.name` two-part and three-part           | first/last split                |
| `basics.name` single word                       | low-confidence name             |
| `basics.profiles[]` LinkedIn/GitHub/other       | correct personal/links mapping  |
| `work[]` with `startDate`/`endDate` ISO strings | dates mapped                    |
| `work[]` with free-text highlights              | achievements/description        |
| `skills[]` with `keywords[]`                    | skills groups/items             |
| `languages[]` with fluency strings              | proficiency mapping             |
| Empty arrays                                    | no crash                        |
| Unknown top-level properties                    | warning or ignored safely       |
| Invalid JSON syntax                             | unreadable                      |
| JSON array at root                              | unsupported structured          |
| Empty object `{}`                               | `import.error.noStructuredData` |

#### ReVitae project JSON

| Scenario                                      | Expected outcome                              |
| --------------------------------------------- | --------------------------------------------- |
| Valid v1 fixture                              | direct round-trip mapping                     |
| Missing `revitaeVersion`                      | unsupported structured (when not JSON Resume) |
| Unsupported version `999`                     | unsupported structured format error           |
| Extra unknown fields                          | ignored safely                                |
| Invalid field types (string instead of array) | readable error, no crash                      |
| Empty sections arrays                         | success with empty sections                   |
| All fields populated fixture                  | high section coverage                         |
| Invalid JSON                                  | unreadable                                    |

#### Europass XML

| Scenario                         | Expected outcome               |
| -------------------------------- | ------------------------------ |
| Minimal valid Europass fixture   | mapped personal/work/education |
| Multilingual content nodes       | preserved text                 |
| Missing optional nodes           | partial import success         |
| Unrecognized Europass subsection | additional info or warning     |
| Invalid XML                      | unreadable                     |
| Wrong namespace/root             | unsupported structured         |

#### HR-XML

| Scenario                              | Expected outcome                            |
| ------------------------------------- | ------------------------------------------- |
| Recognizable resume/candidate fixture | partial mapping                             |
| Unknown HR-XML variant                | unsupported structured                      |
| Missing experience nodes              | personal only still succeeds if data exists |
| Malformed XML                         | unreadable                                  |

#### `SecureXmlReaderFactory`

| Scenario                                                                  | Expected outcome                                      |
| ------------------------------------------------------------------------- | ----------------------------------------------------- |
| Well-formed Europass/HR-XML fixture                                       | parses successfully                                   |
| Malformed XML                                                             | unreadable error                                      |
| XML with external entity reference (`<!ENTITY xxe SYSTEM "file:///...">`) | parse blocked or fails safely; no file content leaked |
| XML with DTD / `DOCTYPE`                                                  | rejected or parsed without entity expansion           |
| Billion-laughs-style nested entity payload (small fixture)                | fails safely without hang or OOM                      |

#### YAML

| Scenario                     | Expected outcome                 |
| ---------------------------- | -------------------------------- |
| JSON Resume equivalent YAML  | same result as JSON path         |
| ReVitae JSON equivalent YAML | same result as ReVitae JSON path |
| Tabs vs spaces               | parsed                           |
| Syntax error                 | unreadable                       |
| Empty document               | empty/no structured data         |
| UTF-8 diacritics             | preserved                        |

#### CSV / TSV

| Scenario                      | Expected outcome                       |
| ----------------------------- | -------------------------------------- |
| Single-row recognized headers | personal fields mapped                 |
| Multiple data rows            | first row imported + multi-row warning |
| Tab delimiter                 | detected                               |
| Comma delimiter               | detected                               |
| Quoted fields with commas     | parsed correctly                       |
| Unknown headers               | unsupported structured                 |
| Header only, no data row      | no structured data                     |
| Empty file                    | empty document error                   |

#### `CvStructuredImportMapper` (shared)

| Scenario                                      | Expected outcome     |
| --------------------------------------------- | -------------------- |
| Direct mapped field → high confidence         | emitted              |
| Heuristic date/name split → medium/low        | emitted              |
| Skipped unknown field → warning               | emitted              |
| `SectionHasData` for partial structured input | correct              |
| Duplicate URLs across personal and links      | deduped with warning |

### Part 5E - Cross-format end-to-end edge cases

`CvDocumentImporterEdgeCaseTests` must include:

| Scenario                                                         | Expected outcome                               |
| ---------------------------------------------------------------- | ---------------------------------------------- |
| Same logical CV content imported via PDF, DOCX, TXT, JSON Resume | equivalent core fields (allow parser variance) |
| Slovak CV through ODT/DOCX/TXT paths                             | diacritics + Slovak headers preserved          |
| Replace-style second import overwrites all sections              | verified via apply helper or model inspection  |
| Import after partial manual edits (unit-level apply simulation)  | full replace semantics                         |
| Large CV (many work entries) across one text format              | completes within test timeout                  |
| Every error key used by importers has at least one test          | coverage guard                                 |

### Part 5F - Test implementation requirements

- Use `[Theory]` + `[InlineData]` / `[MemberData]` for variant matrices where
  appropriate (date formats, encodings, header synonyms).
- Add **`ImportEdgeCaseFixtureFactory`** helpers where binary fixtures are
  generated in tests (similar to existing `ImportPdfFixtureFactory`).
- Each new test class should contain **at least 8 tests**, except tiny wrappers
  where listed scenarios still must all be covered across the class + related classes.
- Do not mark edge-case tests `[Trait("Category", "Slow")]` unless runtime exceeds
  2 seconds; all must run in CI.
- When behavior is intentionally best-effort, assert **bounded outcome** (warning,
  partial field, or safe error) — never silent data corruption.
- Add a short comment above non-obvious edge-case tests explaining the real-world
  CV export that motivated the scenario.

### Required test scenario checklist (summary)

All items below must be covered by the suites above:

- format detection by extension and structured sniffing,
- JSON Resume vs ReVitae JSON disambiguation,
- Europass vs HR-XML vs unknown XML disambiguation,
- every Category A extractor: happy path + empty + corrupt + encoding (where relevant),
- every Category B mapper: happy path + malformed + partial + unsupported version/schema,
- shared parser stages extended without regressing prompt 017 edge cases,
- unsupported extension / image extension manually chosen → unsupported error,
- empty document for each text format,
- file larger than 25 MB → `import.error.fileTooLarge`,
- secure XML parsing rejects XXE/DTD payloads,
- CSV multi-row warning,
- structured import emits `SectionHasData` and confidence entries,
- image-only PDF confirms no OCR attempt,
- all existing import tests continue to pass after refactor.

MainWindow-level UI tests are not required if Core coverage satisfies this matrix.

## Part 6 - Documentation Updates (required)

Documentation is a **required deliverable** of this prompt, not optional follow-up
work. The implementation is incomplete if docs still describe PDF-only import.

Update all user-facing and contributor-facing docs so they match the shipped
behavior exactly.

### Files to update or create

| File                           | Action                                                         |
| ------------------------------ | -------------------------------------------------------------- |
| `README.md`                    | **Major update** — primary user-facing doc (see below)         |
| `docs/concept.md`              | Update current implementation status and import scope          |
| `docs/import-formats.md`       | **Create** — supported format matrix, limits, exclusions       |
| `docs/revitae-project-json.md` | **Create** — native JSON schema for import                     |
| `CHANGELOG.md`                 | Add entry under `[Unreleased]` summarizing multi-format import |

Do not leave outdated PDF-only wording anywhere in the above files.

### README.md — required changes

`README.md` is the most important doc to update. At minimum:

#### 1. Top-level product description

- opening paragraph and value proposition should mention **multi-format import**,
  not PDF-only workflows,
- keep privacy/local-first messaging.

#### 2. Mermaid workflow diagram

Update the import nodes so they are format-neutral, for example:

- `Start fresh or import a PDF` → `Start fresh or import an existing CV`
- `Upload another PDF with replace confirmation` → `Upload another CV file with replace confirmation`

The diagram must still reflect the real flow: intro import, replace confirmation,
structured editing, templates, export.

#### 3. Rename and expand import section

Replace the current **`### PDF Import`** section with a broader section such as
**`### CV Import`**.

That section must include:

- supported formats grouped clearly:
  - **Documents:** PDF, DOCX, DOC, ODT, RTF, TXT, Markdown, HTML, LaTeX, …
  - **Structured:** JSON Resume, ReVitae JSON, Europass XML, YAML, CSV/TSV (limited), …
  - **Limited / best effort:** PAGES, WPS, ABW, HR-XML, legacy DOC, LaTeX — with honest
    wording,
- **explicit exclusions:** scanned/image-only PDFs, photo formats, OCR,
- how import works locally (deterministic parsing, editable draft, confidence hints,
  section expand/collapse),
- startup intro + header upload + replace confirmation,
- **25 MB maximum file size**,
- link to `docs/import-formats.md` for the full matrix and edge-case notes.

Do not dump the entire format matrix into README if `docs/import-formats.md` exists;
README should be concise but complete enough for a new user.

#### 4. `Why ReVitae` / highlights

Update bullets that still imply PDF-only import, for example:

- `PDF import is treated as a draft` → `CV import is treated as a draft`

#### 5. Product Status

Replace wording such as “intro and replace PDF import flows” with multi-format import
language.

Remove or rewrite roadmap items that this prompt implements, especially:

- `More import formats such as DOCX or TXT`

Add only still-open follow-ups (OCR, AI-assisted import, persistence, etc.).

#### 6. Tech Stack

Add new import-related dependencies that were actually introduced, for example:

- DocumentFormat.OpenXml, NPOI, RtfPipe, HtmlAgilityPack, Markdig, YamlDotNet

Keep the list aligned with `ReVitae.Core.csproj`.

#### 7. Repository map (if import paths changed)

Update the `ReVitae.Core/` description if the import folder structure grew
significantly (extractors, structured mappers, XML helpers).

### docs/import-formats.md — required content

Create a contributor/user reference doc containing:

- full supported-format table (extension, support level, notes),
- structured format detection rules (JSON Resume vs ReVitae JSON, Europass vs HR-XML),
- import limits (**25 MB max**),
- security note: XXE-safe XML parsing for untrusted XML inputs,
- explicit out-of-scope list (OCR, images, email attachments, ZIP bundles),
- known limitations per format (PAGES preview-only, WPS may be unsupported, CSV single-row
  shape, etc.),
- brief description of the pipeline:
  `detect format → extract/map → CvImportResult → direct apply`.

### docs/revitae-project-json.md — required content

Document the native ReVitae JSON schema used for import (and future save/load):

- top-level `revitaeVersion`,
- section field names aligned with Core models,
- minimal valid example file,
- unsupported version behavior,
- relationship to JSON Resume (separate schema).

### docs/concept.md — required changes

- update **Current Implementation Status** to reflect multi-format import,
- update Phase 1 / import bullets that still say PDF-only,
- keep AI/persistence items as future work where still accurate.

### CHANGELOG.md

Add an `[Unreleased]` entry summarizing:

- unified `CvDocumentImporter`,
- newly supported formats,
- 25 MB import limit,
- secure XML parsing,
- README/docs updates.

Use complete sentences; follow existing changelog style if present.

### Documentation quality bar

After implementation:

- no README section still titled or written as PDF-only import,
- mermaid diagram matches current UX,
- every format listed in Part 1 of this prompt appears in `docs/import-formats.md`,
- README and docs do not claim OCR/image support,
- README mentions the 25 MB limit,
- new NuGet packages appear in README Tech Stack when added,
- `npm run lint` passes (markdownlint applies to docs).

Keep docs concise, accurate, and aligned with what was actually implemented — do
not document formats or support levels that were intentionally left unsupported.

## Code Reuse Rules

Prefer extending existing import code over rewriting it.

Reuse:

- `CvTextNormalizer`, `CvSectionSegmenter`, `CvImportFieldExtractor`,
- `CvImportResult`, warnings, confidence model,
- intro/replace modals and direct apply flow,
- `ApplyCvImportResult`, section bulk replace APIs,
- localization and validation refresh patterns.

Do not duplicate section parsing logic per format when text extraction is enough.

## Dependency Guidance

Add only .NET packages that are justified and cross-platform.

Suggested packages:

| Package                  | Purpose    |
| ------------------------ | ---------- |
| `DocumentFormat.OpenXml` | DOCX       |
| `NPOI`                   | legacy DOC |
| `RtfPipe`                | RTF        |
| `HtmlAgilityPack`        | HTML       |
| `Markdig`                | Markdown   |
| `YamlDotNet`             | YAML       |

Avoid:

- OCR libraries,
- headless browser/HTML rendering engines,
- native Office binaries,
- cloud conversion APIs.

Document any new dependency licenses in README only if required.

## Out of Scope

Do not implement in this prompt:

- OCR for scanned PDFs or images,
- image file import of any kind,
- AI-assisted parsing or LLM extraction,
- email attachment parsing,
- ZIP batch import of multiple files,
- import preview / diff / confirmation screen,
- local save/load persistence (except defining ReVitae JSON schema for import),
- export to these formats (export remains separate),
- perfect layout reconstruction from Word/HTML/LaTeX,
- automatic translation of imported content,
- online file fetching,
- background import queue,
- password cracking for protected documents.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing tests must pass,
- new multi-format import tests must pass,
- **README.md and required docs updated** (Part 6) — treat missing doc updates as
  an incomplete prompt,
- **every scenario listed in Part 5A–5F must have a corresponding automated test** —
  the prompt is not complete without exhaustive edge-case coverage.

Test-count guidance (minimum):

| Area                                                                     | Minimum new/extended tests |
| ------------------------------------------------------------------------ | -------------------------- |
| Format detection + facade                                                | 20+                        |
| Shared parser stages (normalizer, segmenter, extractor, patterns, dates) | 40+ extended               |
| Text extractors (all formats combined)                                   | 80+                        |
| Structured mappers (all formats combined)                                | 50+                        |
| End-to-end cross-format                                                  | 10+                        |

These are floors, not ceilings. Prefer more `[Theory]` cases over skipping listed
scenarios.

Manual UI checks should include:

- intro modal import accepts DOCX/TXT/JSON and populates the form,
- header upload accepts replacement import for non-PDF formats,
- replace confirmation appears when form has data,
- unsupported file shows localized error with retry path,
- file larger than 25 MB shows localized too-large error,
- empty/scanned PDF still fails with clear message and no OCR attempt,
- imported low-confidence fields still styled,
- section collapse/expand rules still correct,
- language switch updates new import strings,
- light and dark themes remain acceptable,
- README import section and mermaid diagram read correctly on GitHub preview.

## Expected Result

ReVitae should import existing CVs from the full set of practical non-OCR formats
through one unified local import flow.

Users can:

1. choose **Import existing CV** on startup or upload later from the header,
2. pick a supported document or structured file,
3. get an immediately applied structured first draft in the existing form,
4. review low-confidence fields and edit everything manually.

Implementation should centralize import behind `CvDocumentImporter`, reuse the
existing deterministic parser for text-based formats, add structured mappers for
JSON/XML/YAML/CSV, and leave OCR/image import for a future prompt.

Every importer and parser stage must ship with the **exhaustive edge-case test
matrix from Part 5** so regressions in malformed, partial, multilingual, and
cross-format CV inputs are caught in CI.

**README.md**, `docs/import-formats.md`, `docs/revitae-project-json.md`, and related
doc updates from Part 6 must ship with the same change set so users and contributors
see the new import capabilities immediately.
