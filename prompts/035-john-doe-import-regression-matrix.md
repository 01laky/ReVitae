# Prompt 035 - John Doe Import Regression Matrix (50 Variants)

Stop **manual re-import → log → fix → repeat** loops. Replace ad-hoc tuning with a
**deterministic regression matrix**: at test time, generate **50 maximally filled
John Doe CVs**, each with **every section populated** but with a **unique formatting
edge profile**, import each one, and assert that **all structured fields recover
correctly** against a single canonical expected model **and pass the same
post-import form validation the UI runs before export**.

Builds on prompt **033** (ReVitae PDF re-import), **021** (multi-format import),
and the existing `scripts/GenerateJohnDoeStressPdf` stress fixture. Does **not**
replace round-trip JSON import — this prompt targets **heuristic import fidelity**
across layout and formatting stress.

## Goal

When any import parser change lands, one test suite answers:

> “Did we break parsing for any of the 50 known real-world formatting variants?”

Each variant must:

1. Contain **all import sections** with **maximum realistic volume** (same canonical
   dataset as John Doe stress CV — not empty or minimal stubs).
2. Apply **one primary edge-case formatting dimension** that differs from the other
   49 variants (no duplicate edge profiles).
3. Be **generated at runtime** inside tests (no committing binary stress PDFs to git).
4. Be **imported end-to-end** via `CvDocumentImporter.Import(...)` (or the same
   public path the UI uses).
5. Be **validated** against shared canonical expectations — not hand-written
   per-variant assertions.
6. For `Full` / `PdfFull` / `StandardEntryCounts` modes, assert **zero
   post-import form validation errors** via `JohnDoePostImportValidator` (same
   validators as the desktop UI).

## Non-Goals

- AI / LLM parsing,
- OCR / scanned PDFs (prompt 032),
- perfect pixel layout recovery,
- third-party CV templates unrelated to ReVitae export shapes,
- committing generated artifacts to the repository (optional local dump behind
  `REVITAE_IMPORT_DEBUG=1` only),
- asserting validation-error UI chrome (badges, scroll-to-error) — only the
  underlying `FieldValidator` / collection rules via `JohnDoePostImportValidator`.

## Canonical Source of Truth

### 1. Shared dataset builder

Extract the data-building logic from `scripts/GenerateJohnDoeStressPdf/Program.cs`
into a reusable library type in Core:

```text
src/ReVitae.Core/Export/Fixtures/
  JohnDoeStressCvDataset.cs         ← builds CvExportDocument (canonical dataset)

tests/ReVitae.Tests/Import/Fixtures/JohnDoe/
  JohnDoeCanonicalExpectations.cs   ← expected counts + spot-check values
  JohnDoeVariantSpec.cs             ← id, name, format, profile, template?
  JohnDoeVariantCatalog.cs          ← all 50 specs
  JohnDoeVariantGenerator.cs        ← spec → bytes / temp file path
  JohnDoeImportAssertions.cs        ← shared assert helpers
  JohnDoePostImportValidator.cs     ← UI-equivalent form validation on import result
```

**`JohnDoeCanonicalDataset`** must produce (minimum):

| Section                | Count                  | Notes                                                                             |
| ---------------------- | ---------------------- | --------------------------------------------------------------------------------- |
| Personal               | 1                      | John Doe, full title, location, phone, email, 3 URLs, long summary                |
| Work experience        | 20                     | ReVitae `title` + `company · location · type · date` meta lines                   |
| Education              | 12                     | Degree-first + export meta line with institution · location · degree type · dates |
| Skills                 | 12 groups / 115 skills | ReVitae `skill · proficiency · years` preview lines + category headers            |
| Languages              | 12                     | Main line + Reading/Writing/Speaking/Listening sublines                           |
| Certificates           | 24                     | `Professional Certification #NN — …` single-newline blocks                        |
| Projects               | 24                     | `Project Atlas NN — …` + Role / Organization / Date range / URL / Technologies    |
| Links                  | 20                     | `Label — URL` lines                                                               |
| Additional information | 46 paragraphs          | Long prose blocks forcing page breaks                                             |

Keep **one** canonical object graph. Variants only change **rendering**, not business
data (except deliberate edge injections listed below — those injections must still
parse back to the **same logical values**).

### 2. Shared expectations

**`JohnDoeCanonicalExpectations`** exposes:

```csharp
public sealed record JohnDoeCanonicalExpectations(
    PersonalInformationExpectations Personal,
    SectionCountExpectations Counts,
    IReadOnlyList<WorkExperienceSpotCheck> WorkSpotChecks,
    IReadOnlyList<EducationSpotCheck> EducationSpotChecks,
    IReadOnlyList<SkillsGroupSpotCheck> SkillsSpotChecks,
    // … languages, certificates, projects, links spot checks
);
```

**Assertion tiers** (all variants must pass tiers 1–3; tier 4 where applicable):

| Tier                           | What                                                   | Example                                                                                                            |
| ------------------------------ | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------ |
| **1 — Counts**                 | Entry counts per section                               | `Work == 20`, `Education == 12`, `Certs == 24`                                                                     |
| **2 — Personal**               | Identity + contact URLs                                | `FirstName == John`, LinkedIn full URL, GitHub, portfolio, location contains `94107`                               |
| **3 — Spot checks**            | First, middle, last entry per section                  | Work[0] company `Nimbus Cloud Systems`, Work[19] dates; Education[0] MIT; Skills group `Frontend` contains `React` |
| **4 — Format-specific**        | Only when variant profile guarantees it                | Variant “split LinkedIn URL” still yields merged URL                                                               |
| **5 — Post-import validation** | `JohnDoePostImportValidator` for modes that require it | Zero validation errors after import (cert issue dates, work Present, field max lengths)                            |

Do **not** assert full deep equality on 115 skills × 50 variants in every test run
(unless fast enough); use **counts + stratified spot checks** (first / middle / last
/ one random index per section via fixed seed).

## Architecture

```text
JohnDoeVariantCatalog (50 specs)
        │
        ▼
JohnDoeVariantGenerator
   ├─ PDF  → QuestPdfCvExporter + template id + layout profile
   ├─ DOCX → existing visual export writer (if available) + profile
   ├─ MD/TXT/HTML → text renderer with profile
   └─ Synthetic PDF → SidebarLayoutPdfWriter for column/deferred-sidebar edges
        │
        ▼
CvDocumentImporter.Import(tempFile)
        │
        ▼
JohnDoeImportAssertions.AssertMatchesExpectations(result, spec, expectations)
```

### Test entry point

```csharp
public sealed class JohnDoeImportRegressionMatrixTests
{
    public static IEnumerable<object[]> AllVariants =>
        JohnDoeVariantCatalog.All.Select(spec => new object[] { spec });

    [Theory]
    [MemberData(nameof(AllVariants))]
    public void Import_JohnDoeVariant_RecoversCanonicalData(JohnDoeVariantSpec spec)
    {
        using var temp = JohnDoeVariantGenerator.GenerateTempFile(spec);
        var result = CvDocumentImporter.Import(temp.Path);

        Assert.True(result.Success, $"[{spec.Id}] {result.ErrorMessageKey}");
        JohnDoeImportAssertions.AssertMatchesExpectations(result, spec, JohnDoeCanonicalExpectations.Default);
    }
}
```

Optional smoke gate for local dev:

```csharp
[Fact]
public void Import_JohnDoeVariant_SmokeSubset() { /* variants 01, 11, 19, 28 only */ }
```

Mark the full 50-variant theory with `[Trait("Category", "ImportMatrix")]` so CI can
run it in the main suite (preferred) or as a dedicated job if runtime exceeds ~60s.

## The 50 Variants (each unique, all sections full)

Every row uses the **full canonical dataset**. The **Primary edge** column is what
must differ from other variants. Secondary layout choices are allowed but must not
collapse into the same extraction shape as another variant.

| ID     | Name                                   | Output  | Primary edge under test                                                                                                                                                 |
| ------ | -------------------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **01** | `pdf-modern-sidebar-stress`            | PDF     | ModernSidebar, 36+ pages, baseline ReVitae export (current John Doe.pdf shape)                                                                                          |
| **02** | `pdf-classic-sidebar`                  | PDF     | ClassicSidebar template — contact in left column                                                                                                                        |
| **03** | `pdf-clean-top-header`                 | PDF     | CleanTopHeader — title/location in top band, digital block in main                                                                                                      |
| **04** | `pdf-royal-blue-sidebar`               | PDF     | RoyalBlueSidebar — white location text in sidebar                                                                                                                       |
| **05** | `pdf-navy-profile-split`               | PDF     | NavyProfileSplit — title embedded in header prose band                                                                                                                  |
| **06** | `pdf-photo-left-band`                  | PDF     | PhotoLeftBand — title directly under name block                                                                                                                         |
| **07** | `pdf-dark-sidebar-accent`              | PDF     | DarkSidebarAccent — high-contrast sidebar contact labels                                                                                                                |
| **08** | `pdf-executive-blue-sidebar`           | PDF     | ExecutiveBlueSidebar — alternate sidebar width (~34% split)                                                                                                             |
| **09** | `pdf-centered-minimal-single-column`   | PDF     | CenteredMinimal — no sidebar; all sections single column                                                                                                                |
| **10** | `pdf-orange-timeline`                  | PDF     | OrangeTimeline — date-first work layout emphasis                                                                                                                        |
| **11** | `pdf-deferred-sidebar-end`             | PDF     | Synthetic multi-page PDF via `SidebarLayoutPdfWriter` — contact block appended after last main page (regression for prompt 033 §4)                                      |
| **12** | `pdf-split-linkedin-url-lines`         | PDF     | Inject PDF text extraction where LinkedIn URL spans 3–4 lines (`https://` / `www.linkedin.com/...` / handle) — use layout writer or post-process extracted text fixture |
| **13** | `pdf-split-professional-title-lines`   | PDF     | Professional title label value broken after `Architect &`                                                                                                               |
| **14** | `pdf-digital-block-end`                | PDF     | Portfolio + GitHub only under trailing `Digital` pseudo-section (not sidebar Contact)                                                                                   |
| **15** | `pdf-work-meta-split-dates`            | PDF     | Work meta lines split: company line ends with `· 03 / 2025 -` and date continues next line                                                                              |
| **16** | `pdf-education-meta-split-dates`       | PDF     | Education institution meta split before trailing date (`Master's ·` / `09 / 2000 - 06 / 2002`)                                                                          |
| **17** | `pdf-skills-split-proficiency`         | PDF     | Skill lines split: `C# ·` / `Expert ·` / `18 yrs` on separate rows                                                                                                      |
| **18** | `pdf-certificates-single-newline`      | PDF     | Certificates/projects use single `\n` between entries (no `\n\n`) — ReVitae export default                                                                              |
| **19** | `pdf-languages-with-sublines`          | PDF     | Full language export with Reading/Writing/Speaking/Listening sublines (must not inflate language count)                                                                 |
| **20** | `pdf-unicode-nbsp-location`            | PDF     | NBSP + narrow no-break space inside location and institution names                                                                                                      |
| **21** | `txt-work-at-company`                  | `.txt`  | Work headers use `Title at Company` instead of ReVitae meta dots                                                                                                        |
| **22** | `txt-work-dash-company`                | `.txt`  | Work headers use `Company - Title`                                                                                                                                      |
| **23** | `txt-skills-colon-categories`          | `.txt`  | Skills as `Category: a, b, c` (no ReVitae `·` format)                                                                                                                   |
| **24** | `txt-skills-bullet-list`               | `.txt`  | Skills as `- skill` bullets under `General`                                                                                                                             |
| **25** | `txt-education-degree-first-blankline` | `.txt`  | Education entries separated by `\n\n`, degree before institution                                                                                                        |
| **26** | `txt-education-date-first`             | `.txt`  | Traditional layout: date line before degree (must keep 12 entries)                                                                                                      |
| **27** | `md-github-style-headings`             | `.md`   | Markdown H2 section headers (`## Work Experience`)                                                                                                                      |
| **28** | `docx-open-xml-export`                 | `.docx` | Full CV exported via existing DOCX writer — paragraph breaks between entries                                                                                            |
| **29** | `html-export-paragraphs`               | `.html` | HTML export with `<p>` / `<br>` boundaries                                                                                                                              |
| **30** | `txt-crlf-and-emdash-dates`            | `.txt`  | CRLF line endings + em-dash date ranges (`2019 – Present`) + Slovak section labels (`Kontakt`, `Vzdelanie`, …)                                                          |
| **31** | `txt-certificate-issued-split-line`    | `.txt`  | `Issued:` value on the line after the label                                                                                                                             |
| **32** | `txt-certificate-valid-through-label`  | `.txt`  | `Valid through:` expiration label                                                                                                                                       |
| **33** | `txt-certificate-inline-mainline`      | `.txt`  | Certificate name · issuer · dates on one `·` line                                                                                                                       |
| **34** | `txt-certificate-mmm-yyyy-dates`       | `.txt`  | `Feb 2021` / `Valid through: Feb 2024` date tokens                                                                                                                      |
| **35** | `txt-work-present-on-own-line`         | `.txt`  | `Present` on its own line after split end date                                                                                                                          |
| **36** | `txt-summary-at-end`                   | `.txt`  | Profile/summary section moved after all other sections                                                                                                                  |
| **37** | `txt-certificates-before-education`    | `.txt`  | Certificates section precedes education                                                                                                                                 |
| **38** | `txt-uppercase-section-headers`        | `.txt`  | `WORK EXPERIENCE`, `EDUCATION`, … uppercase headers                                                                                                                     |
| **39** | `txt-tab-indented-content`             | `.txt`  | Tab-indented work title lines                                                                                                                                           |
| **40** | `txt-single-newline-work-entries`      | `.txt`  | Work entries separated by single `\n` only                                                                                                                              |
| **41** | `txt-project-labeled-fields`           | `.txt`  | Default ReVitae export with contact-first ordering                                                                                                                      |
| **42** | `txt-education-institution-first`      | `.txt`  | Institution line before degree + location meta                                                                                                                          |
| **43** | `txt-skills-pipe-separated`            | `.txt`  | `Category \| skill, skill` pipe layout                                                                                                                                  |
| **44** | `txt-links-with-bullets`               | `.txt`  | Custom links as `- Label — URL` bullets                                                                                                                                 |
| **45** | `txt-phone-parentheses-format`         | `.txt`  | `(555) 010-2030` phone formatting                                                                                                                                       |
| **46** | `txt-certificate-credential-url-line`  | `.txt`  | Extra `Credential URL:` line before credential ID                                                                                                                       |
| **47** | `txt-work-contract-employment-type`    | `.txt`  | `Contract` employment type in work meta                                                                                                                                 |
| **48** | `txt-mixed-colon-headers`              | `.txt`  | `Contact:` / `Profile:` colon section headers                                                                                                                           |
| **49** | `pdf-forest-green-sidebar`             | PDF     | ForestGreenSidebar template                                                                                                                                             |
| **50** | `pdf-blue-accent-summary`              | PDF     | BlueAccentSummary template                                                                                                                                              |

### Variant rules

- **No empty sections** — generator must fail fast if any section count is zero.
- **Uniqueness** — `JohnDoeVariantCatalog` validates **50** distinct `(Format, PrimaryEdge)`
  keys at static ctor time; duplicate throws.
- **Deterministic** — same seed → same bytes (important for debugging flaky PDF text
  order); document in test output when PdfPig word order varies slightly.
- **Template rotation** — PDF variants 01–10 cover distinct `CvExportTemplateId`
  values; remaining PDF edges use synthetic or modified exporters, not duplicate
  templates.

## Generator Implementation Notes

### Reuse existing exporters

- PDF: `QuestPdfCvExporter` + `CvExportDocument` from `JohnDoeCanonicalDataset`.
- DOCX/HTML/MD/TXT: reuse `CvVisualExportWriter` or equivalent from export pipeline.
- Synthetic PDF: extend `SidebarLayoutPdfWriter` to render **full** John Doe text
  (not only Jane Sidebar minimal fixture) when testing deferred sidebar / URL splits.

### Formatting profiles

Introduce a small strategy interface:

```csharp
internal interface IJohnDoeFormattingProfile
{
    string Id { get; }
    CvExportDocument Apply(CvExportDocument source);           // optional field tweaks
    string RenderText(CvExportDocument doc);                   // for TXT/MD/HTML paths
    PdfLayoutHints? PdfHints { get; }                          // split lines, deferred sidebar, etc.
}
```

Profiles must **not** change expected logical values except where the edge is
**intentional transport corruption** (variants 12–13, 15–17, 20, 30) that the parser
must repair back to canonical values.

### Temp file hygiene

```csharp
public sealed class GeneratedVariantFile : IDisposable
{
    public string Path { get; }
    // delete on dispose; under TestContext temp root
}
```

## Assertion Helper Requirements

**`JohnDoeImportAssertions`** must:

1. Print variant id + format on failure.
2. Assert `result.Success` with localized error key.
3. Run tier 1–3 expectations.
4. For known repair variants (12–17), assert repaired URLs/dates match canonical.
5. Optionally log short summary: `work=20 edu=12 skills=12/115` via
   `ITestOutputHelper` when `[Trait("LogSummary")]` enabled.

### Skills group count tolerance

Until parser reliably yields 12/12 groups for every PDF template, allow
`Assert.InRange(groups, 11, 12)` **only** for PDF variants with a documented comment
linking to prompt 033; text variants must assert exactly **12** groups.

### Work experience date sanity

Spot-check that entry[0] is current/present job and entry[19] is oldest; months/years
must not drift (regression for orphan date fragments in header/sidebar).

## File / Project Layout

```text
scripts/GenerateJohnDoeStressPdf/Program.cs
  └─ thin CLI wrapper calling JohnDoeCanonicalDataset (move shared code to test lib or Core test helpers)

tests/ReVitae.Tests/Import/Fixtures/JohnDoe/
  └─ (files listed above)

tests/ReVitae.Tests/Import/JohnDoeImportRegressionMatrixTests.cs
  └─ Theory over 30 variants

tests/ReVitae.Tests/ReVitae.Tests.csproj
  └─ reference QuestPDF / export projects if not already
```

**Preferred:** move `JohnDoeCanonicalDataset` to `ReVitae.Core` test support or a
small `ReVitae.TestFixtures` project referenced by both the generator script and
tests — avoid duplicating 300 lines of sample data.

## Relationship to Existing Tests

| Existing                              | Action                                                                                                    |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `JohnDoeStressPdfImportEdgeCaseTests` | Keep as optional local fixture test when `John Doe.pdf` exists; matrix variant **01** supersedes it in CI |
| `ReVitaeExportedImportEdgeCaseTests`  | Keep focused unit tests for individual heuristics; matrix integrates them holistically                    |
| `SidebarLayoutPdfWriter` tests        | Keep; matrix variant **11** uses full-data version                                                        |
| Prompt **033**                        | Mark gaps as done when matrix green; link matrix in 033 validation section                                |

## Debugging Workflow

When a matrix variant fails:

1. Read theory output — variant id + tier that failed.
2. Re-run single variant: `--filter "FullyQualifiedName~JohnDoeImportRegressionMatrixTests&DisplayName~pdf-split-linkedin"`.
3. Optionally dump generated bytes:
   `REVITAE_IMPORT_DEBUG=1` + env `REVITAE_IMPORT_MATRIX_DUMP=1` writes to
   `%TEMP%/revitae-matrix/{variant-id}/`.
4. Fix parser once; all affected variants must pass without one-off test edits.

## Temp file cleanup (required)

Generated matrix CVs must **never** accumulate on disk:

1. **`GeneratedJohnDoeVariantFile : IDisposable`** — each generated file lives in
   `%TEMP%/revitae-matrix/{guid}/`. **`Dispose()` deletes the file and directory**
   (recursive). Tests must use `using var generated = JohnDoeVariantGenerator.Generate(spec)`.
2. **`JohnDoeMatrixTempDirectory.CleanupStaleRoots`** — test fixture `Dispose` removes
   orphan directories older than 1 hour (safety net if a test process is killed).
3. **No committed artifacts** — do not check generated PDFs/TXT from the matrix into git.
4. Optional debug dump only when `REVITAE_IMPORT_MATRIX_DUMP=1` (explicit opt-in; still
   document that dumps are manual cleanup).

Add **`Generate_DisposeDeletesTempCvFileAndDirectory`** test asserting file and folder
are gone after `Dispose()`.

## Validation

- [ ] `JohnDoeVariantCatalog.All.Count == 50` and uniqueness guard passes.
- [ ] Each variant generates non-empty file bytes without exception.
- [ ] Full matrix: **52/52** pass on local `dotnet test --filter Category=ImportMatrix`.
- [ ] CI runs full matrix (or smoke + nightly full — document choice in README).
- [ ] `scripts/GenerateJohnDoeStressPdf` still produces root `John Doe.pdf` from shared dataset.
- [ ] README `Import / PDF re-import` section links to this prompt.
- [ ] Prompt **033** updated: validation points to matrix instead of manual John Doe re-import.

## Suggested Implementation Order

1. Extract `JohnDoeCanonicalDataset` + `JohnDoeCanonicalExpectations`.
2. Implement `JohnDoeImportAssertions` with tiers 1–3.
3. Ship variants **21–27** (text/markdown) first — fast feedback, no PdfPig noise.
4. Ship PDF templates **01–10**.
5. Ship synthetic PDF edges **11–20**.
6. Ship DOCX/HTML **28–29**.
7. Add CRLF/Slovak variant **30**.
8. Wire full `[Theory]` + CI trait.

## Out of Scope (follow-ups)

- 30 variants × OCR pipeline,
- mutation testing / fuzzing random bytes,
- parallel snapshot files checked into LFS,
- auto-fixing parser via LLM from failure logs.
