# Changelog

All notable changes to ReVitae are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Local CV project save/load** (prompt **041**): header **Save**, **Save As**, and
  **Open** for `*.revitae.json` projects; dirty-state window title; unsaved-changes
  confirm (Save / Don't save / Cancel) before New CV, Open, import replace, and
  window close; intro **Open saved project** + **Recent projects** list; optional
  autosave recovery file; `projectSettings` block (template, dismissed quality hints,
  section expand state) via `CvProjectSerializer`; **32** new project tests
  (**1305** total).
- **CV image export** (prompt **031**): **Page images** card in the Export modal
  (PNG / JPEG / WebP); delivery as **ZIP archive** or **separate files**; page
  range (all or From–To); scale 1× / 2×; quality slider for JPEG/WebP; live size
  estimate; export progress status; opaque white background via
  `CvImageBackgroundCompositor`; `CvImageExporter` pipeline (QuestPDF → Docnet →
  ImageSharp); **81+** new image export tests (**1273** total).
- **Universal AI CV completion** (prompt **039**): backend-agnostic
  `AiCvCompletionService`; Ollama `POST /api/chat`; extended online chat clients
  (system + user messages); task registry and prompt templates; **Improve with AI**
  on supported quality hints with suggestion modal (Accept / Edit / Cancel); online
  session privacy confirm; EN + SK localization; **37** new AI/CV tests (**1190**
  total).
- **AI provider list and configuration** (prompt **038**): online provider catalog
  (OpenAI, Anthropic, Gemini, Groq, Azure OpenAI, Mistral, DeepSeek, OpenRouter,
  Custom); inline configure / Save / Test forms; single active backend (local **or**
  online); switch and untested-activation confirmations; encrypted `ai-secrets.enc`;
  settings schema v2 with legacy migration; active-backend strip; header badges for
  local (green) and online (blue) active backends; **60+** provider / settings /
  connection tests.
- **Resumable AI model download** (prompt **037**): background Ollama pull via
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
- **AI setup modal** (prompt **036**): header robot icon opens in-window setup
  with loader on every open; local OS/CPU/RAM/disk/Ollama detection; privacy
  banner; **11** curated Ollama instruct models; RAM-tier recommendations;
  optional **one-tier-up** download with warning; disk-space gate before pull;
  Ollama `POST /api/pull` progress; `ai-settings.json` persistence under
  `%LocalAppData%/ReVitae/`.
- Documentation: [`docs/ai-setup.md`](docs/ai-setup.md).
- **John Doe import regression matrix** (prompt **035**): **50** runtime-generated
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
- **Optional profile photo** (prompt **023**): upload JPEG/PNG/WebP (max **15 MB**)
  from Personal information; EXIF auto-orient on save; click-to-replace; local
  storage under `%LocalAppData%/ReVitae/profile-photos/`; template preview +
  PDF/HTML/DOCX embedding; sidebar **initials fallback** when no photo.
- ReVitae JSON/YAML **`revitaeVersion: 2`** with `profilePhotoBase64` /
  `profilePhotoContentType` round-trip (v1 unchanged; absolute paths never exported).
- Profile photo test suites (`ProfilePhotoStorageTests`, `ProfilePhotoInitialsTests`,
  structured/export extensions) — **859 tests** total.
- **Multi-format CV export** (prompt **022**): **Export** toolbar button opens an
  in-window format modal with 15 formats (PDF, DOCX, ODT, RTF, HTML, Markdown,
  TXT, LaTeX, ReVitae JSON, JSON Resume, YAML, Europass XML, HR-XML, CSV, TSV).
- `CvDocumentExporter` facade, `CvExportFormatCatalog`, visual/structured writers,
  save-dialog defaults, post-export **Open file** / **Show in folder** actions,
  and SVG format icons under `src/ReVitae/Assets/ExportFormats/`.
- Export test suites under `tests/ReVitae.Tests/Export/` (783 tests total).
- Documentation: [`docs/export-formats.md`](docs/export-formats.md).
- Unified **multi-format CV import** via `CvDocumentImporter` (prompt **021**): PDF;
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

[Unreleased]: https://github.com/01laky/ReVitae/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/01laky/ReVitae/releases/tag/v0.1.0
