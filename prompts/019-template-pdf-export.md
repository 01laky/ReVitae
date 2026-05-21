# Prompt 019 - Template PDF Export and Download

Replace the current plain-text PDF export with a **template-aligned PDF download**
that matches the user's selected CV template preview.

The exported file should look like a real CV document, not a debug text dump. PDF
is the **only** export format in this prompt.

## Goal

When the user clicks **Export PDF**, ReVitae should:

1. validate the form using the existing combined validation flow,
2. open a native save dialog,
3. generate a polished PDF using the **currently selected template**,
4. write the file locally,
5. show a localized success message.

The preview and exported PDF must use the **same structured CV data** and the
**same selected template**. What the user sees in preview should be what they
get in the downloaded PDF, within reasonable document-layout tolerance.

This prompt builds on:

- the complete structured CV form from prompts 002–016,
- the four Avalonia preview templates from prompt 006,
- inline validation and export scroll-to-first-error from prompt 018,
- intro/replace PDF import from prompt 017,
- existing `StorageProvider.SaveFilePickerAsync` usage in `MainWindow`.

## Current State

Today the app has a split export path:

- **Preview:** rich Avalonia layouts in `MainWindow.axaml.cs`
  (`BuildClassicSidebarTemplate`, `BuildModernSidebarTemplate`,
  `BuildCleanTopHeaderTemplate`, `BuildDarkSidebarAccentTemplate`) driven by
  `CvTemplateData`.
- **Export:** legacy plain-text PDF via `CreatePdfBytes(BuildPreviewLines())`,
  which ignores the selected template and renders one monospace-like text block
  per line.

Additional current limitations:

- save dialog title `"Export PDF"` is hardcoded English,
- suggested filename is always `revitae-basic-cv.pdf`,
- PDF uses built-in Helvetica Type1 text and manual PDF object writing,
- Unicode/diacritics (for example `Kostolný`, `Košice`, `súčasnosť`) are not
  handled reliably,
- long CVs have no pagination strategy,
- export layout logic is duplicated separately from preview layout logic.

This prompt removes the plain-text export path and replaces it with template PDF
export.

## Product Behavior Summary

### Happy path

1. User edits CV data and selects a template in the Templates modal.
2. Preview updates live as today.
3. User clicks **Export PDF**.
4. If validation passes:
   - native save dialog opens,
   - default filename is derived from the candidate name,
   - file type filter is PDF only,
   - app generates PDF bytes from selected template + current form data,
   - file is written locally,
   - status text shows localized success (`Exported PDF to {0}.`).
5. If validation fails:
   - keep existing prompt 018 behavior unchanged,
   - export remains blocked,
   - first invalid field is revealed/scrolled into view,
   - `ExportStatusTextBlock` shows only `ExportFixValidation`.

### UX requirements

- Export button stays disabled while the form is invalid, as today.
- Do not add a second export confirmation modal in v1.
- Do not add export progress UI unless generation becomes visibly slow; if needed,
  use a lightweight busy state on the Export button only.
- Canceling the save dialog is silent (no error message).
- Successful export must not mutate form data.
- Failed write/I/O errors must show a localized error near the Export button.

### Filename suggestion

Suggested save filename should be human-friendly and derived from personal info:

- preferred: `{FirstName}_{LastName}_CV.pdf`
- fallback when name is missing: `ReVitae_CV.pdf`
- sanitize invalid filesystem characters,
- trim whitespace,
- preserve Unicode letters where the OS allows them in filenames.

Example: `Ladislav_Kostolny_CV.pdf`

## Part 1 - Architecture

Introduce a dedicated export layer instead of keeping PDF generation inside
`MainWindow`.

Suggested structure:

```text
src/ReVitae.Core/
  Export/
    CvExportDocument.cs              shared document model consumed by preview + PDF
    CvExportTemplateId.cs
    ICvPdfExporter.cs
    CvPdfExportResult.cs
    CvPdfExportService.cs            orchestration + filename suggestion helpers
    Pdf/
      QuestPdfCvExporter.cs          implementation
      Templates/
        ClassicSidebarPdfTemplate.cs
        ModernSidebarPdfTemplate.cs
        CleanTopHeaderPdfTemplate.cs
        DarkSidebarAccentPdfTemplate.cs

src/ReVitae/
  Export/
    CvExportDocumentFactory.cs       maps live UI/form state -> CvExportDocument
```

Requirements:

- `MainWindow` should orchestrate validation, file picker, and status text only.
- PDF rendering code must be testable without launching the Avalonia UI.
- Preview and export must consume the same document model, not separate ad hoc
  string builders.
- Keep PdfPig import separate from PDF export generation; do not reuse the
  handwritten `CreatePdfBytes()` writer.

### Recommended PDF engine

Use **QuestPDF** for v1 unless a strong repo-specific blocker appears.

Why QuestPDF fits this project:

- strong .NET support,
- good Unicode/text layout,
- page breaks and multi-page documents,
- programmatic layout maps well to the existing four template families,
- easier to test than Avalonia visual tree printing.

Do **not** introduce a headless browser/HTML-to-PDF stack in this prompt.

If QuestPDF is adopted, document the license in README only if required by the
chosen package; do not add unrelated legal docs.

## Part 2 - Shared Export Document Model

Extract the preview data currently represented by the private `CvTemplateData`
record in `MainWindow.axaml.cs` into a shared Core model, for example
`CvExportDocument`.

The model must include everything already rendered in preview across all four
templates:

- personal information and summary,
- work experience entries,
- education entries,
- skills groups,
- languages,
- certificates,
- projects,
- custom links,
- additional information,
- selected template id,
- localized section labels needed by PDF rendering.

Use stable preview-ready structures already produced by:

- `GetActiveWorkExperienceEntries()`,
- `GetActiveEducationEntries()`,
- `GetActiveSkillsPreviewGroups()`,
- etc.

Do not export draft/empty repeatable entries that preview already hides.

Add a factory in the UI layer:

```csharp
CvExportDocumentFactory.Create(
    MainWindow form state,
    AppLocalizer localizer,
    CvExportTemplateId selectedTemplate)
```

Preview rendering should eventually call the same factory rather than rebuilding
similar data inline, but full preview refactor is not required if it would cause
a huge diff. Minimum requirement: **PDF export and preview must use the same
factory output**.

## Part 3 - Template Parity

Implement PDF versions of all four existing templates:

1. Classic Sidebar
2. Modern Sidebar
3. Clean Top Header
4. Dark Sidebar Accent

Parity requirements:

- same section ordering as preview,
- same omission rules for empty optional fields/sections,
- same date-range formatting already used in preview helpers,
- same work/education/project/certificates detail lines where preview shows them,
- sidebar/header color intent preserved as closely as QuestPDF reasonably allows,
- no photo rendering required; layouts must remain balanced without a photo.

Acceptable v1 differences:

- minor spacing/font metric differences,
- preview scroll container vs PDF paginated page breaks,
- subtle color approximation.

Not acceptable:

- missing entire sections that preview shows,
- falling back to plain text export,
- exporting a different template than the one selected in the Templates modal.

## Part 4 - PDF Layout Rules

### Page setup

- use A4 page size,
- reasonable margins suitable for printing,
- support multi-page output automatically,
- avoid clipped text for long descriptions, achievements, projects, and
  additional information.

### Typography

- use a Unicode-capable font strategy,
- ensure Slovak/Czech accented characters render correctly in exported PDFs,
- do not rely on legacy Type1 Helvetica-only writing.

### Section rendering

General rules:

- omit empty sections entirely,
- section headings use localized labels from `AppLocalizer`,
- preserve multiline text with line breaks in descriptions, achievements,
  highlights, and additional information,
- wrap long URLs instead of letting them run off the page when possible.

Work experience / education / projects / certificates:

- show primary header line (title/company/institution/etc.),
- show location/meta/date lines when present,
- show description/achievements/technologies/highlights using the same content
  rules as preview,
- do not export inactive draft cards.

Skills:

- preserve category grouping,
- preserve skill name + proficiency + years when present.

Languages:

- preserve main line and sub-skill lines already used in preview.

Links:

- preserve label/url/note formatting.

Additional information:

- preserve free-form multiline content.

## Part 5 - Export Workflow in MainWindow

Refactor `OnExportPdfClicked` to:

1. validate via existing `ValidateForm()`,
2. on failure, keep prompt 018 export failure behavior,
3. build `CvExportDocument` from current UI state + `_selectedTemplate`,
4. open localized save dialog,
5. call `ICvPdfExporter.Export(document, outputStream)`,
6. handle success/failure status messages.

Remove from `MainWindow` after migration:

- `CreatePdfBytes`,
- `EscapePdfText`,
- `BuildPreviewLines`,
- section-specific `Build*PdfLines()` helpers used only by plain export.

Keep preview builders unless moved as part of shared refactor; do not delete
preview functionality.

### Localization

Add translation keys for any new export UI text, including at minimum:

- save dialog title,
- PDF file type label if not already localized,
- export generation failure message,
- optional export-in-progress text if a busy state is added.

Replace hardcoded `"Export PDF"` save dialog title with localized text.

Add all new keys to:

- `TranslationKeys.cs`
- `TranslationKeys.RequiredKeys`
- `AppLocalizer.cs` English defaults

Suggested keys:

- `export.saveDialogTitle`
- `export.pdfFileType`
- `export.failed`
- `export.inProgress` (optional)

Existing keys to keep using:

- `action.exportPdf`
- `export.fixValidation`
- `export.filePickerUnavailable`
- `export.exportedPdfTo`

## Part 6 - Validation and Export Failure Integration

Do not weaken validation for export.

Export must still require the same active-entry validation as today across:

- personal information,
- work experience,
- education,
- skills,
- languages,
- certificates,
- projects,
- links,
- additional information.

Preserve prompt 018 behavior:

- inline field errors remain the primary feedback,
- export failure scrolls to first invalid field,
- `ExportFixValidation` remains the only generic export failure message.

Do not add a separate export-validation summary panel.

## Part 7 - Error Handling

Handle these export failures explicitly:

| Case | Expected behavior |
| --- | --- |
| invalid form | block export, existing validation UX |
| file picker unavailable | show `export.filePickerUnavailable` |
| user cancels save dialog | no status/error message |
| PDF generation throws | show localized `export.failed`, do not crash |
| stream write/I/O failure | show localized `export.failed` |
| empty document after sanitization | still allowed if personal info exists; do not export completely empty file |

Log or capture exception details internally if the project already has a pattern
for non-user-facing diagnostics; do not show stack traces in UI.

## Part 8 - Tests

Add comprehensive automated tests under `tests/ReVitae.Tests/Export/`.

Required test areas:

### Document factory tests

- maps personal info and all sections from representative form state,
- excludes draft/inactive repeatable entries,
- carries selected template id,
- localizes section labels via supplied `AppLocalizer`,
- filename suggestion sanitization and fallback rules.

### PDF exporter tests

Use QuestPDF testing helpers or byte-level smoke checks plus structural
assertions. At minimum:

- each of the four templates produces non-empty PDF bytes,
- PDF header `%PDF` present,
- exported PDF contains expected Unicode text (for example `Kostolný`, `Košice`),
- long additional-information content produces multi-page output or at least does
  not truncate silently,
- empty optional sections do not leave placeholder junk like repeated `-` lines.

### Regression tests

- invalid form does not invoke exporter,
- existing validation tests remain passing,
- removing plain-text `CreatePdfBytes` path does not break import tests.

### Fixture strategy

Prefer deterministic unit tests with constructed `CvExportDocument` fixtures.
Optional golden-file PDF comparison is nice-to-have but not required in v1 unless
stable across platforms.

## Part 9 - Edge Case Specification

Cover all of the following explicitly in tests or manual QA:

1. names with diacritics in PDF content and suggested filename,
2. very long work experience description wraps across pages,
3. currently-working entries hide end date in PDF the same way as preview,
4. skills with many chips/groups remain readable,
5. projects with technology lists and highlights,
6. custom links with notes,
7. additional information multiline block,
8. all four templates export successfully from the same document data,
9. switching template then exporting uses the new template,
10. export after PDF import of a real two-column CV (for example
    `Ladislav_Kostolny_CV.pdf`) preserves imported content in output,
11. export with only personal info filled still succeeds,
12. cancel save dialog leaves form unchanged and shows no error,
13. read-only failure when output path cannot be written.

## Out of Scope

Do not implement in this prompt:

- DOCX export,
- HTML export,
- JSON export,
- cloud upload/share,
- email send,
- export presets/settings screen,
- watermarking,
- password-protected PDFs,
- embedded photo upload/rendering,
- batch export,
- print dialog integration,
- page size selector beyond A4,
- custom user template import,
- AI layout optimization.

## Acceptance Criteria

The prompt is complete when all of the following are true:

1. Plain-text `CreatePdfBytes(BuildPreviewLines())` export path is removed.
2. Export uses the selected preview template, not a separate text layout.
3. All four templates produce PDF output.
4. Unicode CV content renders correctly in generated PDFs.
5. Save dialog uses localized title and smart default filename.
6. Validation-gated export behavior from prompt 018 remains intact.
7. Export logic lives outside `MainWindow` in dedicated export classes.
8. Preview and export share the same document factory output.
9. New export tests pass alongside the full suite.
10. `npm run lint` and `./scripts/test.sh` pass.
11. README product status mentions template-based PDF export instead of plain PDF
    export.

## Manual QA Checklist

1. Select **Clean Top Header**, fill sample data, export PDF → layout resembles
   preview, not plain text.
2. Switch to **Dark Sidebar Accent**, export again → different layout, same data.
3. Export CV with Slovak diacritics → characters render correctly in PDF.
4. Export imported real CV (`Ladislav_Kostolny_CV.pdf`) → all major sections
   appear in PDF.
5. Leave required field invalid → export blocked, inline errors visible, scroll
   to first invalid field works.
6. Cancel save dialog → no error message.
7. Save to valid folder → localized success message appears.
8. Open exported PDF in Preview/Adobe/browser → multiple pages work for long CV.

## Suggested Implementation Order

1. Add `CvExportDocument`, template id enum, filename helper, and factory from UI
   state.
2. Introduce QuestPDF dependency and `ICvPdfExporter` abstraction.
3. Implement one template end-to-end (recommended: **Clean Top Header**) to prove
   pipeline.
4. Port remaining three templates.
5. Refactor `OnExportPdfClicked` to use new exporter and localized save dialog.
6. Remove legacy plain-text PDF code from `MainWindow`.
7. Add export tests and README/status updates.
8. Run full lint/test suite and manual QA with imported + manually edited CVs.

## Expected Result

ReVitae should feel complete as a CV builder: users edit structured data, preview
a polished template, and download a matching PDF file ready to send to employers.
PDF export becomes a first-class feature aligned with the product concept, not a
temporary plaintext debug output.
