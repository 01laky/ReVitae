# Changelog

All notable changes to ReVitae are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **Refactor foundation (047):** enforced a **warning-free build** via
  `TreatWarningsAsErrors` (and cleared the residual nullable warnings); added a **golden
  render oracle** (`CvTemplateRenderSignature` with `CvTemplateRenderGoldenTests`) that pins
  every template's deterministic layout signature so future rendering refactors are provably
  behaviour-preserving; added `docs/architecture.md` module map.
- **Refactor (047 T3):** split the 2 477-line `ImportFieldExtractionCore` helpers god file
  into five focused partial files (`ImportExtractionHelpers.{,.Education,.Merging,.WorkAndSkills,.Names}.cs`),
  each ≤ ~620 LOC, behaviour-preserving (no call-site changes; guarded by the import matrix +
  golden oracle). Test total **2085**.
- **Refactor (047 T8):** centralized the reused neutral PDF colours (`White`, `Black`,
  `MutedOnDark`, `AvatarNeutral`) into `CvPdfPalette`, replacing inlined hex literals across
  10 template files. Same hex values → **pixel-identical** render (verified via PNG hash).
- **Refactor (047 T2 — drag-reorder):** replaced six byte-identical `MoveEntryToIndex`
  copies across the section views with a single generic, unit-tested
  `SectionEntryReorder` helper (`MoveToIndex<T>` + `FindIndexById<T>`; 19 edge-case tests), replacing six + four byte-identical copies. Test total **2104**.

- **Refactor (047 T5 — orphan-key audit):** added `TranslationKeyOrphanAuditTests` that
  flags `TranslationKeys` constants unreferenced by production source. The current 45
  unreferenced keys (month/year field labels, reserved AI labels, legacy strings) are an
  explicit allow-list; the test fails on any **new** orphan so dead strings cannot accumulate.
  Removal of the existing ones is deferred (some are reserved for imminent UI use).
- **Refactor (047 T4 + QG5):** extracted the preview content hash into a tested
  `CvExportDocumentHash.Compute` (Core) used by the preview cache; added `.editorconfig`
  code-quality guardrails (unused using/member, static-able member, readonly, brace style)
  so the smells this refactor removed do not silently return.
- **Refactor (047 T1 — unified template rendering):** the live preview now rasterizes the
  **actual export PDF** (`CvTemplatePreviewImage` → QuestPDF → Docnet raster → per-page PNG)
  instead of a parallel Avalonia re-implementation of every template. Preview is now
  **guaranteed to match the export**, updates are debounced (~220 ms) and run off the UI thread,
  cached by document content hash, and serialized for pdfium safety. Removed the ~1 745-line
  `MainWindow.TemplatePreviews.*` Avalonia layout duplication. **Needs interactive QA** of
  preview responsiveness/appearance.
- **Refactor (047 T6 — template scaffold):** added `CvPdfRenderHelper.RenderPage` (the shared
  `Generate → Page → ConfigureA4Page` scaffold) and routed all 16 base templates plus the
  themed renderer through it, removing the repeated boilerplate. Pixel-identical render
  (verified via PNG hash, incl. non-white backgrounds) and golden oracle unchanged.

### Fixed

- **Template sidebar bands now span the full page height on every page.** Previously a
  coloured sidebar paired with longer main content left a blank gap below the band on
  continuation pages. Added `CvPdfLayoutHelpers.ComposeFullHeightSidebarPage` (paints the
  band via `page.Background()`, full-bleed, aligned to the content column) and applied it
  to the themed **LeftSidebarLight / RightSidebarLight / FullSidebarDark / AccentBarLeft**
  layouts (~20 templates) plus the base **ClassicSidebar, ModernSidebar, DarkSidebarAccent,
  ExecutiveBlueSidebar** templates. Audited all 56 templates via a reusable preview
  generator (`scripts/GenerateTemplatePreviews`).

## [0.2.12] - 2026-06-10

### Added

- **Prompt 045 — AI section advice & proactive import assist:**
  - **Broadened AI advice** beyond the original five hints: `personal.summary-too-long`
    → `ShortenProfessionalSummary`; `skills.single-large-group` → `SuggestSkillGrouping`;
    `skills.section-empty` → `DraftSkillsFromContext`; `education.section-empty` /
    `languages.section-empty` → advice-only tasks (never fabricate degrees/levels).
  - **Per-section advisor** (`AiCvCompletionService.AdviseSectionAsync`) — proactive
    1–4 review-only suggestions for Summary, Work, Skills, Education, Languages, Projects,
    even with no static hint; `AiCvSectionContent` + `AiCvAdvisorGate` min-content gate.
  - **Targeted import field repair** (`AiCvImportFieldRepairService`) — corrects only
    low-confidence fields (`AiImportFieldRepairPlanner`, cap **25**, lowest-confidence
    first, transparent "N more" disclosure) instead of a full re-extract; adds/removes
    no entries; preserves photos/ids.
  - **Broader import triggers** — `DeterministicPartial` (3–4 sections) and
    `DeterministicHasLowFields` flags on `AiCvImportTriggerEvaluator`.
  - **Relevance & safety guards:** optional target-role/JD context (`AiCvTargetContext`),
    `AiCvEntityGuard` anti-hallucination post-check on rewrites, `AiCvContentLanguageDetector`
    (rewrites stay in the CV language, tips in UI language), per-suggestion rationale,
    one-level `AiCvApplyUndoBuffer`, session LRU `AiCvAdvisorCache`, and sanitized
    `AiCvDiagnosticsLogger` (`ai-advisor` / `ai-repair` steps).
  - **UI:** per-section **Ask AI for tips** buttons (Work, Education, Skills,
    Languages, Projects) via shared `SectionHeaderBadges`; dedicated advisor modal
    with rationale lines, cached indicator, Refresh, and online-send confirm; session
    target-role / job-description inputs; one-level **Undo** bar; entity-guard warning
    and broadened advice-list hints routed through the suggestion modal. The
    **Enhance with AI** import banner now fires on partial (3–4 section) and
    low-confidence parses via the new trigger flags.
  - **Fix fields with AI** — import banner button + per-field before→after review
    modal (with cap "N more" disclosure) and one-level undo, wired for resolvable
    low-confidence **personal-information** fields; unresolvable fields are skipped.
  - **EN + SK** localization for all new strings; **2081** total tests (+226),
    including an extensive edge-case layer across every new component (entity guard,
    content-language detector, advisor cache/gate/undo, advice parsing, repair
    planner/parser/prompt/service, trigger evaluator, advisor service).

### Changed

- `AiCvCompletionService.CompleteForQualityHintAsync` now accepts an optional
  `AiCvTargetContext` and returns an `EntityGuard` payload for rewrite tasks.
- **Version** bumped to **0.2.12**; test-count baseline raised to **2081**.

## [0.2.11] - 2026-05-21

### Added

- **Prompt 044 — refactoring & edge-case audit (full):**
  - `CvProjectLifecycleService` with `IClock` / `IProjectAutosaveStore` — autosave debounce,
    recovery evaluation, shutdown-safe writes; `CvProjectPathValidator` for open/save paths.
  - Split `CvImportFieldExtractor` into `Import/Extraction/*` section modules + thin
    orchestrator (~75 lines); seeded `ImportExtractionFuzzEdgeCaseTests`.
  - `FirstLaunchAiWizardController` in Core; UI wizard wired to controller state.
  - Ollama runtime abstractions (`IOllamaServeSupervisor`, `IOllamaProcessLauncher`) and
    direct installer/startup/path edge-case suites.
  - Export content depth tests (HTML escape, structured round-trip, DOCX photo, PDF smoke).
  - Quality / validation UI glue tests; AI service edge cases; `FieldSchemaFactory` tests.
  - Extended `ReVitaeLocalDataPaths` (tessdata, profile photos, import debug log, recent projects).
  - `scripts/verify-test-count.sh` + `TestCountBaselineTests` drift guard (**1845** total,
    +244 from 0.2.4).

### Changed

- **MainWindow.Projects** delegates dirty/autosave/recovery to `CvProjectLifecycleService`.
- **README:** CI category map (`Projects`, `Ollama`, `ImportExtraction`), test-count guard docs.

## [0.2.4] - 2026-05-21

### Added

- **Technical debt hardening:** serial `ImportPdfSerialCollection` for PDF import and
  John Doe matrix tests; TierB sidebar stress repeat test; `GeneratedJohnDoeVariantFile`
  write validation; `scripts/verify-vulnerable-packages.sh` and CI gate;
  `scripts/pre-commit-fast.sh` (`REVITAE_FAST_PRECOMMIT=1` skips matrix locally);
  Ubuntu import-matrix **3×** PDF flake guard; first-launch wizard doc screenshots;
  **12** edge-case tests in `tests/ReVitae.Tests/Import/` and
  `VerifyVulnerablePackagesScriptTests` (**1601** total).

### Changed

- **NuGet security:** explicit pin `System.Security.Cryptography.Xml` **10.0.6**
  (overrides NPOI 2.7.5 transitive **8.0.2** / NU1903).
- **PDF re-import tests:** variants **02** / **07** covered via matrix + stability suite
  only (deduplicated from `ReVitaePdfReimportEdgeCaseTests`).
- **README:** CI test category map, release/tag workflow, fast pre-commit docs.

### Fixed

- **Flaky pre-commit:** intermittent `import.error.unreadableDocument` on ClassicSidebar
  variant **02** under parallel full-suite load.
- **CS9191** in `CvExportFormatIconLoader` (`in` vs `ref` for SkiaSharp matrix).

## [0.2.3] - 2026-05-21

### Added

- **First-launch AI setup wizard:** multi-step overlay on cold start (before the
  intro) with Welcome → Choose path → Local or Online setup → Complete. Four
  paths: local Ollama download, curated online providers (OpenAI, Anthropic,
  Gemini, Groq), **Remind me later**, and **I won't use AI** (hides Try AI /
  Enhance with AI promotions). Persists to `%LocalAppData%/ReVitae/app-settings.json`
  (schema v2); upgrade migration auto-completes when an active AI backend or
  resumable download job already exists. Setup modal link **Show AI setup wizard
  again**; Welcome step language shortcut opens Setup and returns. EN + SK
  localization; **42** edge-case tests in `tests/ReVitae.Tests/AppPreferences/`
  (**1591** total).

### Changed

- **AI setup detection:** shared `RunAiSetupDetectionAsync` visibility guard so
  hardware detection works from both the AI modal and the first-launch wizard.

## [0.2.2] - 2026-05-21

Also included in the **0.2.2** codebase (initial release notes were incomplete):

### Added

- **40 themed CV templates:** parametric QuestPDF layouts (10 layout archetypes ×
  distinct color themes) — **56** built-in templates total with live preview and PDF export.
- **Edge-case test hardening:** OCR composite routing, quality-gate boundaries,
  tessdata discovery, import session/progress, YAML importer facade, Slovak OCR
  localization, and related suites (see commits from `33d4090` onward).
- **GitHub Actions CI:** lint + test on Ubuntu, macOS, and Windows; dedicated John Doe
  **import-matrix** job on Ubuntu.

### Added (release notes)

- **Author metadata:** Ladislav Kostolny and contact email in the About dialog,
  `Version.props`, npm `package.json`, README, and assembly metadata
  (`AppVersion.Author` / `AuthorEmail`).

### Changed

- **Tab indentation:** C#, AXAML, MSBuild, and shell scripts use tabs project-wide;
  `.editorconfig` documents the convention for future edits.
- **CI format check:** `lint-cs.sh` verifies `dotnet format` on the UI project
  (`ReVitae.csproj`) in addition to Core and Tests.

## [0.2.1] - 2026-05-25

### Added

- **OCR and image CV import (completion):** bundled `eng.traineddata` in app
  output; `OcrLanguageResolver` (UI culture → Tesseract language packs);
  **Import as scan (OCR)** retry on failed PDF imports without reopening the file
  picker; dedicated **Images** file-picker group; Slovak OCR strings; committed
  fixtures under `tests/.../Fixtures/Ocr/` and generator
  `scripts/GenerateOcrImportFixtures/`; edge-case and `OcrIntegration` tests
  (**1428** total).

### Changed

- **README:** visitor-focused copy — benefit-led intro, format table, simplified
  feature sections, and developer import notes moved to a dedicated subsection.

## [0.2.0] - 2026-05-25

### Added

- **ReVitae PDF round-trip import**: QuestPDF export metadata
  fingerprint (`Producer`, `Creator`, `Keywords: template:*`); `ReVitaePdfLayoutProfile`
  and template-aware PdfPig column split; `ReVitaePdfExportHints` threaded through
  extraction → `CvImportFieldExtractor`; John Doe matrix expanded to **51** variants
  (tier **B** for PDF **02**, **07**, **49**; deferred-sidebar **51**); committed
  `JohnDoeStressCv.pdf` fixture; **40+** new/tightened tests in
  `tests/ReVitae.Tests/Import/Pdf/`, `ReVitaePdfReimportEdgeCaseTests`, and export
  metadata suites (**1417** total).
- **AI-assisted CV import**: `CvTextImportCoordinator` retains
  normalized text on deterministic failure; `AiCvImportService` with model-aware
  batch profiles (Compact → ExtraLarge), sequential phased extraction, review
  summary diff, Try AI / Enhance with AI UI, online session confirm reuse; EN + SK localization; **`docs/ai-import.md`**; **71** new tests in
  `tests/ReVitae.Tests/Ai/Import/` (**1376** total).
- **Local CV project save/load**: header **Save**, **Save As**, and
  **Open** for `*.revitae.json` projects; dirty-state window title; unsaved-changes
  confirm (Save / Don't save / Cancel) before New CV, Open, import replace, and
  window close; intro **Open saved project** + **Recent projects** list; optional
  autosave recovery file; `projectSettings` block (template, dismissed quality hints,
  section expand state) via `CvProjectSerializer`; **32** new project tests
  (**1305** total).
- **CV image export**: **Page images** card in the Export modal
  (PNG / JPEG / WebP); delivery as **ZIP archive** or **separate files**; page
  range (all or From–To); scale 1× / 2×; quality slider for JPEG/WebP; live size
  estimate; export progress status; opaque white background via
  `CvImageBackgroundCompositor`; `CvImageExporter` pipeline (QuestPDF → Docnet →
  ImageSharp); **81+** new image export tests (**1273** total).
- **Universal AI CV completion**: backend-agnostic
  `AiCvCompletionService`; Ollama `POST /api/chat`; extended online chat clients
  (system + user messages); task registry and AI message templates; **Improve with AI**
  on supported quality hints with suggestion modal (Accept / Edit / Cancel); online
  session privacy confirm; EN + SK localization; **37** new AI/CV tests (**1190**
  total).
- **AI provider list and configuration**: online provider catalog
  (OpenAI, Anthropic, Gemini, Groq, Azure OpenAI, Mistral, DeepSeek, OpenRouter,
  Custom); inline configure / Save / Test forms; single active backend (local **or**
  online); switch and untested-activation confirmations; encrypted `ai-secrets.enc`;
  settings schema v2 with legacy migration; active-backend strip; header badges for
  local (green) and online (blue) active backends; **60+** provider / settings /
  connection tests.
- **Resumable AI model download**: background Ollama pull via
  `AiModelDownloadCoordinator`; bottom-left progress dock; modal download banner
  with Pause / Resume / Stop; startup auto-resume with exponential backoff;
  atomic `ai-download-job.json`; header robot badge during active download;
  ~4 s success dwell; disk-full recovery messaging; monotonic
  `AiDownloadDisplayProgress`; **50+** focused download / lifecycle / Ollama tests.
- **Managed Ollama install**: `OllamaInstaller` with resumable zip download,
  `OllamaServeSupervisor`, and managed paths under `%LocalAppData%/ReVitae/ollama/`
  when no system Ollama is present.
- **AI model lifecycle**: per-card installation status (downloaded / downloading /
  stale); **Remove model** and **Clean up failed download** via
  `AiModelLifecycleService` and Ollama `DELETE /api/delete`.
- **AI setup modal**: header robot icon opens in-window setup
  with loader on every open; local OS/CPU/RAM/disk/Ollama detection; privacy
  banner; **11** curated Ollama instruct models; RAM-tier recommendations;
  optional **one-tier-up** download with warning; disk-space gate before pull;
  Ollama `POST /api/pull` progress; `ai-settings.json` persistence under
  `%LocalAppData%/ReVitae/`.
- Documentation: [`docs/ai-setup.md`](docs/ai-setup.md).
- **John Doe import regression matrix**: **51** runtime-generated
  stress CV variants (PDF templates, TXT/MD/HTML/DOCX profiles) imported via
  `CvDocumentImporter`; shared `JohnDoeStressCvDataset` in Core; matrix asserts
  extraction fidelity **and** zero post-import form validation errors
  (`JohnDoePostImportValidator`).
- **Quality hint modal**: section badge opens a large centered in-window modal
  (replacing the small flyout) with readable typography and Escape/close actions.
- Import edge-case tests: institution-first education blocks, inline certificate
  headers with issuer and dates.
- **16 CV templates** with QuestPDF export, live preview, template picker cards,
  thumbnails, and localized names/descriptions.
- Dedicated **About** modal (toolbar icon) with version badge and early-preview
  label; **Setup** modal now handles language selection only.
- Shared validation helpers: `FieldSchemaFactory`, `CollectionEntryValidationHelper`,
  `CvFormatImporterRegistry`, Core `CvExportSourceDataFactory`.
- Expanded edge-case tests (889 total): field format enums, Europass/HR-XML mappers,
  template catalog, profile photo bytes, import error normalization, format detection,
  RTF/LaTeX/ODT/DOC/ABW/Pages text extractors, HR-XML export round-trip,
  `MonthYearSelection`, `CvExportDocumentMapper`.
- **Optional profile photo**: upload JPEG/PNG/WebP (max **15 MB**)
  from Personal information; EXIF auto-orient on save; click-to-replace; local
  storage under `%LocalAppData%/ReVitae/profile-photos/`; template preview +
  PDF/HTML/DOCX embedding; sidebar **initials fallback** when no photo.
- ReVitae JSON/YAML **`revitaeVersion: 2`** with `profilePhotoBase64` /
  `profilePhotoContentType` round-trip (v1 unchanged; absolute paths never exported).
- Profile photo test suites (`ProfilePhotoStorageTests`, `ProfilePhotoInitialsTests`,
  structured/export extensions) — **859 tests** total.
- **Multi-format CV export**: **Export** toolbar button opens an
  in-window format modal with 15 formats (PDF, DOCX, ODT, RTF, HTML, Markdown,
  TXT, LaTeX, ReVitae JSON, JSON Resume, YAML, Europass XML, HR-XML, CSV, TSV).
- `CvDocumentExporter` facade, `CvExportFormatCatalog`, visual/structured writers,
  save-dialog defaults, post-export **Open file** / **Show in folder** actions,
  and SVG format icons under `src/ReVitae/Assets/ExportFormats/`.
- Export test suites under `tests/ReVitae.Tests/Export/` (783 tests total).
- Documentation: [`docs/export-formats.md`](docs/export-formats.md).
- Unified **multi-format CV import** via `CvDocumentImporter`: PDF;
  TXT/Markdown/HTML; DOC/DOCX; ODT/RTF; AbiWord, Pages, WPS, LaTeX; Json Resume;
  native `.revitae.json`; YAML; CSV/TSV; Europass / HR‑XML-style XML when detected.
- **25 MB** import size guard (`CvImportLimits`) and **XXE-safe XML** parsing
  (`SecureXmlReaderFactory`) for office-derived XML surfaces.
- Structured mappers (`JsonResumeMapper`, `ReVitaeJsonMapper`, tabular, Europass,
  HR‑XML) plus text extractors registered behind `ICvFormatImporter`.
- Targeted import edge-case suites under `tests/ReVitae.Tests/Import/`.
- Documentation: [`docs/import-formats.md`](docs/import-formats.md) format matrix
  and [`docs/revitae-project-json.md`](docs/revitae-project-json.md) native
  interchange schema.
- **Documentation screenshots**: nine English-named UI captures in `docs/img/`
  embedded in README and product docs (editor, AI setup, export, templates,
  quality hints).

### Changed

- AI download pause/resume: progress callbacks no longer overwrite **Paused** state;
  `PauseAsync` waits for pull cancellation; UI progress throttle runs on the UI
  thread with a fixed refresh interval.
- Certificate import: labeled fields, inline `·` headers, credential ID lines no
  longer mis-parse as year-only dates; expiration and split `Issued:` lines.
- Work import: standalone `Present` line after split date ranges.
- Education import: institution-first ReVitae blocks use paragraph splitting and
  location-only meta lines.
- HTML import: block-level line breaks and preserved `<pre>` whitespace.
- John Doe stress dataset: summary capped at 800 chars, additional information
  capped for import round-trip within form limits.
- `scripts/GenerateJohnDoeStressPdf` reuses `JohnDoeStressCvDataset` from Core.
- Modal top-right close buttons use an **X** icon instead of text **Close**.
- `MonthYearValue` moved to `ReVitae.Core.Cv` (shared date type).
- `MainWindow` split into partials: export document builder, shared preview helpers,
  base template layouts (Extended/Templates/ProfilePhoto unchanged).
- Core `CvExportDocumentMapper` and `MonthYearSelection`; UI `MonthYearDateHelper`
  delegates month/year conversion to Core.
- `ExpandableSection` fires `ExpandStateChanged` only when expand/collapse toggles.
- `HrXmlMapper` imports ReVitae HR-XML export output (Email, EmploymentHistory blocks).
- Preview section helpers call `CvExportPreviewContentBuilder` directly (removed thin wrappers).
- **Export PDF** renamed to **Export**; validation-gated format modal replaces
  direct PDF save dialog; localized status and file-type labels updated for all
  formats.
- Intro and header **replace import** flows now accept all supported formats (not
  PDF-only); UI copy and file picker filters updated accordingly.
- README, concept doc, and roadmap aligned with multi-format import scope.

### Fixed

- Post-import validation failures after PDF import (certificate issue dates,
  work end dates, summary/additional length) addressed via parser and dataset tuning.
- **YAML structured import:** numeric/boolean YAML scalars map to JSON numbers
  again (fixes native ReVitae YAML round-trip).
- Education import no longer creates duplicate garbage entries when PDF text
  extraction splits a single institution name across blank lines (continuation
  blocks such as `and Training` / `Engineering` merge into one entry).

## [0.1.0] - 2026-05-21

First formally versioned ReVitae release baseline.

### Added

- Structured CV builder with personal information, summary, work experience,
  education, skills, languages, certificates, projects, custom links, and
  additional information.
- Inline field validation UI with section badges and export scroll-to-first-error.
- Intro and replace PDF import flows with deterministic parsing and low-confidence
  review highlighting.
- Four live preview templates and matching QuestPDF export.
- Setup modal with language selection; About/version information in a separate About modal.
- Centralized app versioning via `Version.props`, README app badge, and release
  verification scripts.

### Changed

- Replaced legacy plain-text PDF export with template-aligned PDF generation.
- Improved two-column PDF import parsing for contact details, education dates,
  sidebar skill bleed filtering, and work-experience technology detection.

[Unreleased]: https://github.com/01laky/ReVitae/compare/v0.2.2...HEAD
[0.2.2]: https://github.com/01laky/ReVitae/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/01laky/ReVitae/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/01laky/ReVitae/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/01laky/ReVitae/releases/tag/v0.1.0
