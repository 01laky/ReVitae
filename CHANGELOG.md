# Changelog

All notable changes to ReVitae are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
