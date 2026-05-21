# Prompt 017 - Intro Modal and PDF Import

Add a startup intro modal that lets the user either create a new CV or import an
existing CV from a PDF file. When the user imports a PDF, extract plain text from
the document, parse it with a deterministic rule-based parser, and populate the
existing structured CV form sections.

Sections that receive **no imported data** must be **collapsed by default** after
import. Sections that receive data must remain expanded so the user can review
what was detected.

This prompt introduces the first end-to-end import path. It intentionally uses
**heuristic parsing without AI**. AI-assisted import belongs to a later phase.

## Goal

Support both primary user journeys from the product concept:

1. **Create a new CV from scratch** — close the intro modal and show the existing
   empty structured form.
2. **Import an existing CV** — pick a PDF, extract text, parse it into structured
   section data, populate the form, and collapse empty sections.

This step should build on:

- the complete Phase 1 structured CV form from prompts 002–016,
- existing in-window modal patterns from prompts 004–005 and 010,
- Avalonia `StorageProvider` file picker usage already present for PDF export,
- `ExpandableSection` expand/collapse behavior,
- validation, preview, localization, and Material-styled UI conventions.

The intro modal should appear **when the application starts**, before the main CV
workspace becomes interactive.

## Product Behavior Summary

### Startup flow

1. Application window opens.
2. Intro modal is visible immediately.
3. Main form and preview are visible behind a de-emphasized backdrop but are not
   interactive until the user completes the intro step.
4. User chooses one of two actions:
   - **Create new CV**
   - **Import from PDF**
5. If the user chooses **Create new CV**:
   - close the intro modal,
   - keep the current empty form state,
   - all sections use the existing default expanded behavior for a blank CV.
6. If the user chooses **Import from PDF**:
   - open a native file picker filtered to PDF files,
   - read and parse the selected file,
   - populate matching form sections,
   - collapse sections with no imported data,
   - close the intro modal,
   - refresh preview and validation.

### Import quality expectations

Parsing will not be perfect. The goal is a **useful first draft**, not flawless
structure recovery.

The UI must communicate this clearly:

- imported content is always editable,
- the user should review imported fields,
- low-confidence or unmapped text may land in `Additional Information` or remain
  unassigned with a localized warning summary.

Do not promise AI-level extraction accuracy in this prompt.

## Part 1 - Intro Modal

## Modal Shell

Add a new in-window intro modal using the same overlay pattern as the existing
setup, templates, and preview-expand modals.

Suggested XAML names:

- `IntroModalOverlay`
- `IntroModalPanel`

Requirements:

- rendered inside the existing Avalonia window,
- no separate native OS window,
- semi-transparent backdrop over the full app,
- centered modal panel,
- responsive sizing relative to the current window,
- visible on application startup (`IsVisible = True` initially),
- blocks interaction with the form and preview until dismissed through a valid
  user action,
- does **not** reset existing modal states for setup/templates/preview when
  opened,
- supports light and dark theme using existing app styling conventions.

Suggested modal size:

- narrower than the 80% setup/templates modals,
- approximately **520–640 px** max width on desktop,
- auto height based on content,
- on small windows, respect minimum margins and allow vertical scrolling inside
  the modal body if needed.

Do **not** allow closing the intro modal with `Escape`. Update `OnWindowKeyDown` so
`Escape` does not dismiss the intro modal while it is visible. The user should make
a deliberate choice between creating a new CV or importing.

While the intro modal is visible, block opening or interacting with setup, templates,
and preview-expand modals behind it.

Do **not** show a generic close button that dismisses the modal without choosing
an action.

## Intro Modal Content

The modal body should contain:

1. localized title,
2. short explanatory subtitle,
3. two primary action cards or buttons:
   - **Create new CV**
   - **Import from PDF**
4. optional short helper text about supported import limitations.

Suggested English copy direction:

- title: `Welcome to ReVitae`
- subtitle: `Create a new CV from scratch or import an existing PDF to get started
  faster.`
- create action: `Create new CV`
- import action: `Import from PDF`
- helper: `PDF import works best with text-based CVs. Scanned image-only PDFs are
  not supported yet.`

Use Material-friendly layout:

- clear visual hierarchy,
- icon for each action (`FilePlus` / `FileUpload` or equivalent from
  `MaterialIconFactory`),
- primary emphasis on both actions without making one feel hidden.

## Import In-Progress State

After the user selects **Import from PDF** and chooses a file, the intro modal
should switch to an in-modal progress state before closing.

Requirements:

- disable both action buttons while import is running,
- show localized progress text such as `Reading PDF…` and `Parsing CV…` if practical,
- show a simple progress indicator/spinner,
- prevent duplicate file picker launches,
- on success, close the intro modal and apply imported data,
- on failure, stay in the intro modal and show a localized error message with a
  way to retry import or choose **Create new CV**,
- if the user cancels the file picker, return to the initial intro modal state
  without showing an error.

Import work must run asynchronously so the UI thread stays responsive during PDF
reading and parsing. Use `async`/`await` from the intro modal click handler through
`MainWindow`.

## Direct Apply Policy

Do **not** add an intermediate import preview or confirmation step.

After a successful parse:

1. immediately apply all detected data to the structured form,
2. close the intro modal,
3. refresh preview and validation.

The user reviews and edits imported content directly in the normal form sections.
There is no separate “review import” screen, no diff view, and no second confirm
button before apply.

## Accessibility

Requirements:

- meaningful automation names for both actions,
- localized tooltips where icons are used,
- keyboard focus moves into the modal on startup,
- action buttons are keyboard activatable,
- import error text is readable and associated with the modal body.

## Part 2 - PDF Text Extraction

## Extraction Library

Add a PDF text extraction dependency suitable for cross-platform desktop use.

Recommended library:

- **`UglyToad.PdfPig`** in `ReVitae.Core`

Add the package reference to `src/ReVitae.Core/ReVitae.Core.csproj`.

Rationale:

- pure .NET,
- no native binary dependency management,
- adequate for text-based PDF CVs,
- easy to unit test through wrapper abstraction.

Suggested files:

- `src/ReVitae.Core/Import/Pdf/IPdfTextExtractor.cs`
- `src/ReVitae.Core/Import/Pdf/PdfPigTextExtractor.cs`
- `src/ReVitae.Core/Import/Pdf/PdfTextExtractionResult.cs`

Suggested interface:

```csharp
public interface IPdfTextExtractor
{
    PdfTextExtractionResult Extract(string filePath);
}
```

Suggested result shape:

```csharp
public sealed record PdfTextExtractionResult(
    bool Success,
    string Text,
    int PageCount,
    string? ErrorMessageKey);
```

Use translation keys such as `import.error.emptyPdf`, not raw exception text, in
`ErrorMessageKey`.

Extraction rules:

- read all pages in order,
- concatenate page text with `\n\n` between pages,
- preserve line breaks where the library provides them,
- trim trailing whitespace,
- if no text is extracted, return failure with error key `import.error.emptyPdf`,
- do not attempt OCR in this prompt.

## Extraction Error Cases

Handle at least:

- file not found,
- unreadable/corrupt PDF,
- password-protected PDF,
- zero extractable text,
- unexpected extractor exception.

These should map to localized import error messages, not raw exception strings.

## Part 3 - CV Parser Architecture

Implement a deterministic, testable import pipeline in `ReVitae.Core`.

The parser must **not** live in UI code. `MainWindow` should orchestrate file pick,
extraction, parsing, and form application only.

## High-Level Pipeline

```text
PDF file path
  → PdfPigTextExtractor
  → CvTextNormalizer
  → CvSectionSegmenter
  → CvImportFieldExtractor
  → CvImportResult
  → MainWindow applies result to section views
```

Suggested facade:

- `src/ReVitae.Core/Import/CvPdfImporter.cs`

Suggested method:

```csharp
public CvImportResult ImportFromPdf(string filePath)
```

Suggested supporting files:

- `src/ReVitae.Core/Import/PersonalInformationImport.cs`
- `src/ReVitae.Core/Import/CvTextNormalizer.cs`
- `src/ReVitae.Core/Import/CvSectionSegmenter.cs`
- `src/ReVitae.Core/Import/CvImportFieldExtractor.cs`
- `src/ReVitae.Core/Import/CvImportResult.cs`
- `src/ReVitae.Core/Import/CvImportSectionId.cs`
- `src/ReVitae.Core/Import/CvImportWarning.cs`
- `src/ReVitae.Core/Import/CvImportConfidence.cs`
- `src/ReVitae.Core/Import/ImportedFieldConfidence.cs`
- `src/ReVitae.Core/Import/Patterns/CvImportPatterns.cs`

## CvImportResult

Suggested personal-info payload:

```csharp
public sealed class PersonalInformationImport
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ProfessionalTitle { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string LinkedInUrl { get; init; } = string.Empty;
    public string PortfolioUrl { get; init; } = string.Empty;
    public string GitHubUrl { get; init; } = string.Empty;
    public string ShortSummary { get; init; } = string.Empty;

    public bool HasAnyData()
    {
        return !string.IsNullOrWhiteSpace(FirstName)
            || !string.IsNullOrWhiteSpace(LastName)
            || !string.IsNullOrWhiteSpace(ProfessionalTitle)
            || !string.IsNullOrWhiteSpace(Email)
            || !string.IsNullOrWhiteSpace(Phone)
            || !string.IsNullOrWhiteSpace(Location)
            || !string.IsNullOrWhiteSpace(LinkedInUrl)
            || !string.IsNullOrWhiteSpace(PortfolioUrl)
            || !string.IsNullOrWhiteSpace(GitHubUrl)
            || !string.IsNullOrWhiteSpace(ShortSummary);
    }
}
```

Suggested aggregate result:

```csharp
public sealed class CvImportResult
{
    public bool Success { get; init; }
    public string? ErrorMessageKey { get; init; }

    public PersonalInformationImport Personal { get; init; }
    public IReadOnlyList<WorkExperienceEntry> WorkExperienceEntries { get; init; }
    public IReadOnlyList<EducationEntry> EducationEntries { get; init; }
    public IReadOnlyList<SkillsGroupEntry> SkillsGroups { get; init; }
    public IReadOnlyList<LanguageEntry> LanguageEntries { get; init; }
    public IReadOnlyList<CertificateEntry> CertificateEntries { get; init; }
    public IReadOnlyList<ProjectEntry> ProjectEntries { get; init; }
    public IReadOnlyList<LinkEntry> LinkEntries { get; init; }
    public string AdditionalInformationContent { get; init; }

    public IReadOnlyDictionary<CvImportSectionId, bool> SectionHasData { get; init; }
    public IReadOnlyList<CvImportWarning> Warnings { get; init; }
    public IReadOnlyList<ImportedFieldConfidence> FieldConfidences { get; init; }
}
```

Each parsed field that is written into the form should also emit a confidence entry
in `FieldConfidences`, keyed with the same field keys used by validation
(for example `firstName`, `links.{entryId}.url`).

Notes:

- reuse existing entry model types from current CV sections where possible,
- imported entries should be valid instances with new stable IDs,
- fields with unknown values remain empty rather than inventing data,
- `SectionHasData` drives post-import expand/collapse behavior.

Suggested section IDs:

- `PersonalInformation`
- `Summary` (parsed separately; counts toward `PersonalInformation` expand state via `ShortSummary`)
- `WorkExperience`
- `Education`
- `Skills`
- `Languages`
- `Certificates`
- `Projects`
- `Links`
- `AdditionalInformation`

## Step 1 - Text Normalization

`CvTextNormalizer` should prepare raw PDF text for parsing.

Responsibilities:

- normalize `\r\n` to `\n`,
- collapse repeated spaces where safe,
- trim trailing spaces on each line,
- collapse 3+ consecutive blank lines to 2,
- normalize common bullet characters (`•`, `◦`, `▪`, `-`, `*`) to a canonical
  bullet marker internally,
- normalize unicode dashes to `-`,
- optionally strip obvious page numbers / footer noise when repeated across pages.

Output: normalized plain text string.

## Step 2 - Section Segmentation

`CvSectionSegmenter` splits the document into logical sections using heading
heuristics.

### Header detection rules

A line is a probable section header when:

- it is relatively short (for example ≤ 60 characters),
- it is not an email/URL/date-only line,
- it matches a known section keyword set,
- it is often uppercase or title case,
- it may end with an optional colon.

Compare lines case-insensitively after trimming punctuation.

Suggested keyword map:

| Section | Example header keywords |
| --- | --- |
| Summary | `summary`, `profile`, `about me`, `professional summary`, `objective`, `profil`, `zhrnutie`, `o mne` |
| Work experience | `work experience`, `employment`, `employment history`, `professional experience`, `pracovné skúsenosti`, `kariéra` |
| Education | `education`, `academic background`, `vzdelanie`, `štúdium` |
| Skills | `skills`, `technical skills`, `core competencies`, `technologies`, `zručnosti`, `technológie` |
| Languages | `languages`, `language skills`, `jazyky`, `jazykové zručnosti` |
| Certificates | `certificates`, `certifications`, `licenses`, `certifikáty`, `certifikácie` |
| Projects | `projects`, `selected projects`, `personal projects`, `projekty` |
| Links | `links`, `online profiles`, `odkazy` |
| Additional | `additional information`, `interests`, `hobbies`, `volunteering`, `awards`, `publications`, `dodatočné informácie`, `záujmy` |

Segmentation algorithm:

1. Split normalized text into lines.
2. Identify header lines and assign each a `CvImportSectionId`.
3. Everything before the first recognized header becomes the **header block**
   (used for personal/contact parsing).
4. Each header starts a section body that continues until the next header.
5. If no headers are found:
   - treat the whole document as header block + one `AdditionalInformation` body,
   - add warning `import.warning.noSectionsDetected`.

Priority when multiple keywords match:

- use the longest matching keyword,
- require whole-line or whole-phrase matches where possible; do not treat isolated
  words such as `experience` inside a sentence as section headers,
- if still ambiguous, prefer more specific multi-word headers over generic single
  words.

Treat `Summary` content as `PersonalInformation.ShortSummary` for form application
and expand/collapse logic.

## Step 3 - Field Extraction by Section

`CvImportFieldExtractor` converts segmented text blocks into typed entry models.

Extraction should be conservative: **prefer partial correct data over hallucinated
structure**.

### Header block → Personal information

Extract from the top of the document:

| Field | Heuristics |
| --- | --- |
| Email | RFC5322-like regex, first strong match |
| Phone | phone regex supporting `+`, spaces, parentheses |
| LinkedIn URL | contains `linkedin.com` |
| GitHub URL | contains `github.com` |
| Portfolio URL | first remaining `http(s)://` URL not matched above |
| Location | line containing comma-separated city/country pattern or labeled `Location:` |
| Professional title | short line near top, often under name, not contact info |
| First / last name | first 1–2 non-empty lines before contact markers; split on last space for two-token names |
| Short summary | only if a dedicated summary section was **not** detected; otherwise summary section wins |

Name parsing is low-confidence. If uncertain:

- put full top line into `FirstName` and leave `LastName` empty, or
- leave both empty and add warning `import.warning.nameUncertain`.

Do not overwrite with guessed names when confidence is low.

### Summary section

If a summary/profile section exists:

- join section body lines into `ShortSummary`,
- preserve paragraph breaks as `\n`.

### Work experience section

Split section body into entry blocks.

Entry boundary heuristics:

- blank line between blocks,
- line containing a date range,
- line that looks like `Job Title at Company`,
- bullet clusters under a title/company header.

Date range patterns to support initially:

- `01/2020 - 03/2024`
- `Jan 2020 - Mar 2024`
- `2020 - 2024`
- `2020 - Present`
- `01/2020 – current`
- localized `súčasnosť` / `present`

For each block extract:

- job title,
- company,
- location (optional),
- start/end month/year when parseable,
- `IsCurrentlyWorking` when end token indicates present/current,
- description from paragraph text,
- achievements from bullet lines,
- technologies from trailing line like `Technologies: C#, .NET, SQL`

If only one line and bullets exist, treat first line as title/company combined and
split on ` at ` / ` · ` / ` | ` when present.

Create one `WorkExperienceEntry` per detected block with imported fields only.

### Education section

Similar block splitting to work experience.

Degree keywords:

- `Bachelor`, `Master`, `PhD`, `BSc`, `MSc`, `Ing.`, `Bc.`, `diploma`, `degree`

Extract:

- degree,
- institution,
- field of study after `in` / `-` patterns,
- location,
- start/end dates using same date parser,
- description from free text.

### Skills section

Support common CV formats:

1. **Comma-separated single line** → one default group named `General` or localized
   import default category key.
2. **Category lines** such as `Programming: C#, JavaScript` → one skills group per
   category.
3. **Bullet list** → skills in default group.

Create `SkillsGroupEntry` and nested skill items using existing skills models.

Proficiency and years should remain empty unless explicitly parsed from patterns
like `C# (advanced, 5 years)`.

### Languages section

Support lines such as:

- `English — Fluent`
- `Slovak (Native)`
- `German - B2`

Extract:

- language name,
- proficiency when mappable to `LanguageProficiency`,
- CEFR level when explicit and mappable to `CefrLevel`.

Suggested mapping examples:

| Detected text | Map to |
| --- | --- |
| `native`, `mother tongue` | `LanguageProficiency.Native` |
| `fluent`, `full professional` | `LanguageProficiency.Fluent` |
| `advanced`, `upper intermediate` | `LanguageProficiency.Advanced` |
| `intermediate`, `working proficiency` | `LanguageProficiency.Intermediate` |
| `elementary`, `basic`, `beginner` | `LanguageProficiency.Elementary` |
| `A1` … `C2` | matching `CefrLevel` |

If no safe mapping exists, leave proficiency/CEFR empty rather than guessing.

### Certificates section

Split by blank lines or bullet entries.

Extract when present:

- certificate name,
- issuer,
- issue date,
- credential URL,
- description/note.

Expiration date may remain empty in Phase 1 import.

### Projects section

Split by blank lines or project title lines.

Extract when present:

- project name,
- role,
- organization/context,
- dates,
- project URL,
- description/highlights from bullets.

Technologies may be parsed from `Tech:` / `Stack:` lines into project technology
items.

### Links section

Import only **additional** links not already placed in personal info.

Rules:

- URLs already assigned to LinkedIn/GitHub/Portfolio in personal info must not be
  duplicated into `Links`,
- each remaining URL becomes a `LinkEntry`,
- label inferred from hostname (`behance.net` → `Behance`) or line prefix before URL.

### Additional information section

Join section body into `AdditionalInformationContent`.

Also append:

- unrecognized trailing sections,
- parser `unmapped` text if needed.

## Step 4 - Shared Parsing Utilities

Add reusable helpers in `CvImportPatterns` or dedicated small classes:

- `EmailPattern`
- `PhonePattern`
- `UrlPattern`
- `DateRangeParser`
- `BulletLineParser`
- `NameHeuristics`
- `LabelFromUrlHeuristics`

Suggested date parser output:

```csharp
public sealed record ParsedDateRange(
    int? StartMonth,
    int? StartYear,
    int? EndMonth,
    int? EndYear,
    bool IsPresent);
```

Month names should support English abbreviations initially (`Jan`, `Feb`, …).

## Step 5 - Warnings and Confidence

Add non-fatal warnings instead of failing import when structure is partial.

### Confidence model

Add:

```csharp
public enum CvImportConfidence
{
    High,
    Medium,
    Low
}

public sealed record ImportedFieldConfidence(
    string FieldKey,
    CvImportConfidence Confidence);
```

Every imported field value should receive a confidence rating from the parser.

Suggested rules:

| Confidence | When to use |
| --- | --- |
| `High` | strong pattern match (`email` regex, exact URL host match, explicit labeled field such as `Email:`) |
| `Medium` | reasonable heuristic match (date range parsed, section block split confidently, language proficiency mapped) |
| `Low` | weak guess (name from first line, title/company split from one combined line, category inferred from hostname only) |

Do not emit confidence entries for fields left empty.

The confidence model is advisory only. It must **not** change validation rules or
block export.

### Low-confidence field UI

After direct apply, visually mark imported fields with `Low` confidence so the user
knows what to review first.

Requirements:

- use a subtle existing-friendly style, such as a dedicated `UiClasses` import-hint
  border/background on the affected input,
- apply confidence styling in section views through a focused API such as
  `ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence>)`,
- personal-info fields in `MainWindow.axaml` should receive the same treatment,
- remove confidence styling when the user edits that field,
- do not add badges, tooltips, or extra copy blocks for every low-confidence field
  in this prompt unless needed for accessibility.

`Medium` confidence fields do not need special styling in this prompt.

### Warning keys

Examples:

- `import.warning.noSectionsDetected`
- `import.warning.nameUncertain`
- `import.warning.workExperiencePartial`
- `import.warning.unmappedTextAppended`
- `import.warning.personalLinksDuplicatedSkipped`

Import should still succeed if at least some usable data was extracted, even when
warnings are present.

If parsing completes but no structured fields were extracted, return
`Success = false` with error key `import.error.noStructuredData`.

Fail import only when:

- PDF text extraction fails,
- parser throws unexpectedly and cannot recover,
- extracted text is empty after normalization.

## Part 4 - Apply Import to Form

## MainWindow Orchestration

Update:

- `src/ReVitae/MainWindow.axaml`
- `src/ReVitae/MainWindow.axaml.cs`

Add startup logic:

1. show intro modal on window open,
2. handle create/import actions,
3. on import, call `StorageProvider.OpenFilePickerAsync` with PDF filter,
4. run `CvPdfImporter.ImportFromPdf(path)`,
5. on success, **immediately** apply the result via `ApplyCvImportResult(CvImportResult)`
   without an intermediate review/confirm step,
6. close the intro modal and refresh preview/validation.

Suggested PDF file picker filter:

- display name localized, for example `PDF files`
- pattern `*.pdf`

## Replace Form State Safely

Applying import should replace current in-memory form content.

Requirements:

- clear existing entries in all repeatable sections before loading imported entries,
- reset personal information text fields before applying imported personal fields,
- rebuild section views from imported models,
- trigger preview and validation refresh after apply,
- do not leave stale data from a previous in-session blank form.

Because persistence is not implemented yet, this replacement behavior is acceptable
for startup import.

## Section Expand/Collapse Rules After Import

This is a core product requirement.

Default today: sections and entry cards are expanded. After import, override as
follows:

| UI section | Expanded after import when |
| --- | --- |
| Personal information | `PersonalInformationImport.HasAnyData()` is true |
| Work experience | one or more imported work entries exist |
| Education | one or more imported education entries exist |
| Skills | one or more imported skill groups exist |
| Languages | one or more imported language entries exist |
| Certificates | one or more imported certificate entries exist |
| Projects | one or more imported project entries exist |
| Links | one or more imported custom link entries exist |
| Additional information | imported content is non-whitespace |

Otherwise the top-level section must be **collapsed**.

### Entry-level collapse inside repeatable sections

For imported repeatable entries:

- entry cards with imported data: expanded,
- if a section has imported entries, empty draft placeholder entries must **not** be
  auto-added during import.

Extend entry cards or bulk-load APIs so imported entries can be created already
expanded, for example:

- `ReplaceEntries(IReadOnlyList<TEntry> entries, bool expandSection = true)`, and
- entry cards created in expanded state when their entry `HasUserInput()` is true.

Do not auto-insert blank draft rows as part of import.

### Post-import validation behavior

Imported entries with partial data may immediately become **active** under existing
draft/active rules and show validation errors for missing required fields. This is
expected.

Requirements:

- do not weaken existing validators for import,
- run `UpdateValidationState()` after apply,
- optionally scroll/focus the first populated section so the user can review imported
  content quickly.

### Create new CV path

When the user chooses **Create new CV**:

- keep current default behavior,
- all sections remain expanded as they are today for an empty form.

## Section View API Extensions

Current section views do not expose bulk load or expand-state setters. Extend them
as needed with minimal focused APIs.

Suggested additions per repeatable section view:

- `ReplaceEntries(IReadOnlyList<TEntry> entries, bool expandSection = true)`,
- `SetSectionExpanded(bool isExpanded)`.

Also add for personal info / additional information:

- personal info fields applied directly in `MainWindow` via a helper such as
  `ApplyPersonalInformationImport(PersonalInformationImport personal)`,
- `AdditionalInformationSectionView.SetContent(string content)` and
  `SetSectionExpanded(bool)`.

For `PersonalInformationSection` (`ExpandableSection` in `MainWindow.axaml`), set
`PersonalInformationSection.IsExpanded` directly after import.

Implementation notes:

- bulk replace should rebuild cards once,
- suppress redundant `EntriesChanged` storms during apply if practical (apply silently
  then single refresh),
- preserve existing validation behavior after import,
- after apply, call `ApplyImportConfidence(result.FieldConfidences)` on section views
  and personal-info inputs.

## Part 5 - Internationalization

Add translation keys for all intro/import UI and error/warning messages.

Add keys to:

- `src/ReVitae.Core/Localization/TranslationKeys.cs`
- `src/ReVitae.Core/Localization/AppLocalizer.cs`

Include at least:

- intro modal title/subtitle/helper,
- create/import action labels,
- import progress messages,
- import error messages (`import.error.*`),
- import warning messages (`import.warning.*`),
- PDF file picker label,
- default imported skills category label if used.

Every supported language must receive the new required translation keys.

Do not hardcode intro/import strings in XAML or code-behind.

## Part 6 - Unit Tests

Add comprehensive tests under `tests/ReVitae.Tests/Import/`.

Suggested files:

- `CvTextNormalizerTests.cs`
- `CvSectionSegmenterTests.cs`
- `CvImportFieldExtractorTests.cs`
- `CvPdfImporterTests.cs`
- `DateRangeParserTests.cs`
- `PdfPigTextExtractorTests.cs`

### Text fixtures

Use plain-text fixtures for most parser unit tests.

Suggested location:

- `tests/ReVitae.Tests/Import/Fixtures/Text/`

Example fixture categories:

1. classic two-column-exported plain CV text,
2. CV with only work experience and education,
3. CV with comma-separated skills,
4. CV with no section headers,
5. CV with LinkedIn/GitHub URLs in header,
6. multilingual headers (`Vzdelanie`, `Pracovné skúsenosti`),
7. messy extra blank lines and bullets.

### Sample PDF fixtures (required)

Add a small set of committed **text-based PDF** files to verify real PdfPig
extraction and end-to-end import behavior.

Suggested location:

- `tests/ReVitae.Tests/Import/Fixtures/Pdf/`

Required sample files:

| File | Purpose |
| --- | --- |
| `sample-cv-en-basic.pdf` | simple one-column English CV with contact info, summary, work experience, education, skills |
| `sample-cv-sk-basic.pdf` | Slovak section headers (`Profil`, `Pracovné skúsenosti`, `Vzdelanie`, `Jazyky`) |
| `sample-cv-en-messy.pdf` | extra blank lines, bullets, combined title/company lines, trailing unmapped text |

Requirements for sample PDFs:

- text-based only (no scanned images),
- small file size (keep each fixture minimal),
- generated and committed intentionally for test stability,
- include a short comment in test code or fixture README describing what each PDF
  is expected to extract,
- mark tests that depend on them as integration-style if runtime is higher, but they
  must run in CI.

`PdfPigTextExtractorTests` should assert at minimum:

- non-empty extracted text,
- expected page count,
- presence of key strings such as email or section headers.

`CvPdfImporterTests` should include at least one end-to-end test per sample PDF
that asserts plausible structured output, for example:

- personal email detected in `sample-cv-en-basic.pdf`,
- at least one work experience entry detected,
- Slovak headers parsed in `sample-cv-sk-basic.pdf`,
- low-confidence fields emitted where expected in `sample-cv-en-messy.pdf`.

Do not rely only on text fixtures for PDF extraction coverage.

Tests should cover at least:

- email/phone/URL extraction,
- section header detection,
- work experience block splitting and date parsing,
- education parsing,
- skills comma and category formats,
- language line parsing,
- duplicate personal URLs not re-added to links,
- warnings for uncertain name / no sections,
- `SectionHasData` mapping,
- empty PDF extraction failure path,
- parser success with partial data,
- confidence ratings emitted for parsed fields,
- low-confidence personal name styling cleared after user edit,
- PDF sample fixtures extract and import successfully.

Do not add optional-only PDF coverage; the three sample PDF fixtures above are
required for this prompt.

## Code Reuse Rules

Prefer extending existing patterns over inventing new ones.

Reuse where practical:

- modal overlay structure from setup/templates modals,
- `StorageProvider` file picker approach from PDF export,
- existing entry models and validators,
- `ExpandableSection.IsExpanded`,
- `UiClasses`, `MaterialIconFactory`,
- localization and validation refresh flow from `MainWindow`.

Keep the diff focused on intro modal and PDF import only.

## Out of Scope

Do not implement these in this prompt:

- AI-assisted parsing or LLM extraction,
- DOCX/TXT import,
- OCR for scanned PDFs,
- local persistence / resume last project,
- skipping intro modal on subsequent launches,
- import preview diff UI,
- import confirmation/review step before applying parsed data,
- post-import review banner or warning panel,
- batch import of multiple files,
- cloud upload,
- automatic template switching after import,
- perfect layout reconstruction from PDF,
- editing or rewriting imported content automatically,
- import history/versioning,
- background import jobs.

## Validation and Quality Bar

After implementation:

- `./scripts/format-cs.sh` must pass,
- `./scripts/lint-cs.sh` must pass,
- `npm run lint` must pass,
- all existing unit tests must pass,
- new import/parser tests must pass.

Manual UI checks should include:

- intro modal appears on app startup,
- create new CV closes modal and shows empty expanded form,
- import opens PDF file picker,
- valid text-based PDF populates form immediately after modal closes,
- empty sections collapse after import,
- populated sections stay expanded,
- personal LinkedIn/GitHub/portfolio fields fill when detected,
- custom links section does not duplicate personal URLs,
- preview updates after import,
- validation runs after import,
- import error stays in modal with retry path,
- file picker cancel returns to intro choices without error,
- low-confidence imported fields show subtle review styling,
- import with partial entries may show expected validation errors,
- translations visible after language change,
- light and dark theme both look acceptable.

## Expected Result

ReVitae should present a startup intro modal with two clear paths: **Create new CV**
and **Import from PDF**.

Import should:

1. extract text from a user-selected PDF,
2. parse it through a deterministic pipeline,
3. **directly** populate the existing structured CV form,
4. collapse sections that received no data,
5. mark low-confidence imported fields for review,
6. leave all imported content fully editable.

This delivers the first usable import workflow while keeping a clean path for future
AI-assisted extraction in Phase 2.
