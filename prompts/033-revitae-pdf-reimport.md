# Prompt 033 - ReVitae Template-Aware PDF Re-Import

Improve **PdfPig text extraction and parsing** for PDFs **exported from ReVitae**
(sidebar templates, multi-page stress exports). This is **separate from OCR**
(prompt 032): when a ReVitae export has a text layer, PdfPig should parse it
accurately without falling back to OCR.

## Goal

Re-importing a ReVitae-exported PDF (e.g. Modern Sidebar, 36-page stress CV)
should recover:

- personal fields (title, location, LinkedIn, GitHub) from sidebar contact blocks,
- skills **categories** and counts (not one flattened group),
- education institutions (not garbled fragments),
- certificates and projects at full export counts.

Round-trip fidelity for power users remains **`*.revitae.json`**. This prompt
targets **PDF re-import quality** for the common “export PDF → edit elsewhere →
re-import PDF” workflow.

## Priority

**Higher than OCR** for ReVitae-owned exports: OCR cannot restore column layout
on rendered bitmaps. Fix PdfPig + parser heuristics first.

## Known gaps (John Doe.pdf stress fixture)

| Field                             | Expected        | Current (approx.)       |
| --------------------------------- | --------------- | ----------------------- |
| Work                              | 20              | 20 ✓                    |
| Languages                         | 12              | 12 ✓                    |
| Skills groups                     | 12 / 115 skills | 1 / 84                  |
| Education                         | 12              | 4 (split institutions)  |
| Certificates                      | 24              | 4                       |
| Projects                          | 24              | 10                      |
| Title, Location, LinkedIn, GitHub | set             | empty (URL line breaks) |

## Implementation areas

### 1. Sidebar contact URL merge

When sidebar contact lines break URLs across rows (`linkedin.com/in/` + handle),
merge before regex extraction in `CvImportFieldExtractor` or post-process sidebar
text in `PdfPigTextExtractor`.

### 2. ReVitae skills format

When `reVitaeSkillFormat=true` (or detected category headers from export), preserve
skill **groups** instead of flattening to one category.

### 3. Education block splitting

Tune `CvImportFieldExtractor.ExtractEducation` for ReVitae export line breaks
(degree / institution / dates on separate lines without over-splitting).

### 4. Section body boundaries

Ensure deferred sidebar append does not starve main-column sections on pages 2+.

### 5. Regression tests

- `ReVitaeExportedSidebarCv.pdf` — existing round-trip tests stay green.
- `John Doe.pdf` (or synthetic 36-page sidebar PDF) — assert improved counts
  for skills, education, certificates, projects, contact URLs.

## Out of scope

- OCR fallback (prompt 032),
- AI / LLM parsing,
- perfect fidelity for arbitrary third-party PDF layouts.

## Validation

- Full test suite passes.
- Manual re-import of exported sidebar PDF shows contact URLs and skill categories.
