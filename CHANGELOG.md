# Changelog

All notable changes to ReVitae are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Multi-format CV export** (prompt **022**): **Export** toolbar button opens an
  in-window format modal with 15 formats (PDF, DOCX, ODT, RTF, HTML, Markdown,
  TXT, LaTeX, ReVitae JSON, JSON Resume, YAML, Europass XML, HR-XML, CSV, TSV).
- `CvDocumentExporter` facade, `CvExportFormatCatalog`, visual/structured writers,
  save-dialog defaults, post-export **Open file** / **Show in folder** actions,
  and SVG format icons under `src/ReVitae/Assets/ExportFormats/`.
- Export test suites under `tests/ReVitae.Tests/Export/` (728 tests total).
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

- **Export PDF** renamed to **Export**; validation-gated format modal replaces
  direct PDF save dialog; localized status and file-type labels updated for all
  formats.
- Intro and header **replace import** flows now accept all supported formats (not
  PDF-only); UI copy and file picker filters updated accordingly.
- README, concept doc, and roadmap aligned with multi-format import scope.

### Fixed

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
- Setup modal with language selection and About/version information.
- Centralized app versioning via `Version.props`, README app badge, and release
  verification scripts.

### Changed

- Replaced legacy plain-text PDF export with template-aligned PDF generation.
- Improved two-column PDF import parsing for contact details, education dates,
  sidebar skill bleed filtering, and work-experience technology detection.

[Unreleased]: https://github.com/01laky/ReVitae/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/01laky/ReVitae/releases/tag/v0.1.0
