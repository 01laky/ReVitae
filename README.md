# ReVitae

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.0-blue)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-macOS%20%7C%20Windows%20%7C%20Linux-lightgrey)](https://github.com/01laky/ReVitae)
[![Tests](https://img.shields.io/badge/tests-397%20passing-brightgreen)](https://github.com/01laky/ReVitae)

ReVitae is a privacy-conscious desktop CV builder for creating, importing,
editing, previewing, and exporting professional CVs.

It keeps the CV content structured and editable, while templates handle only the
visual presentation. The goal is simple: spend time improving your CV, not
wrestling with formatting.

```text
Start fresh or import a PDF
          |
          v
Fill structured CV sections
          |
          v
Switch preview templates anytime
          |
          v
Export a polished PDF
```

## Why ReVitae

Most CV workflows mix content and layout together. ReVitae separates them.

- Your CV data is the source of truth.
- Templates can change without losing content.
- Imported data stays editable.
- The app runs locally by default.
- PDF import is treated as a draft, not as magic.

## Current Highlights

### Structured CV Builder

ReVitae includes dedicated form sections for the core CV content:

- Personal information and professional summary
- Work experience
- Education
- Skills
- Languages
- Certificates
- Projects
- Additional custom links
- Additional information

Each section has focused validation, repeatable entries where needed, live
preview updates, and localized UI text.

### PDF Import

On startup, ReVitae lets you either create a new CV or import an existing PDF.

The importer extracts text locally, applies deterministic parsing rules, and
populates the structured form directly. Sections with imported data are expanded
for review, while empty sections are collapsed.

The import flow currently supports text-based PDFs. Scanned image-only PDFs and
OCR are not supported yet.

### Template Preview

You can switch between multiple built-in preview templates without changing your
CV content.

Current template styles include:

- Classic Sidebar
- Modern Sidebar
- Clean Top Header
- Dark Sidebar Accent

The preview can be expanded into a larger modal and scrolls independently from
the form.

### Validation and Review

The app validates fields while you work:

- Required personal and section fields
- Date ranges
- URL formats
- Duplicate entries where relevant
- Maximum field lengths
- Imported low-confidence fields highlighted for review

## Product Status

ReVitae is an active early-stage desktop app. The structured CV form and basic
PDF workflow are in place. The next major product areas are local persistence,
more polished template-based export, and smarter import/recommendation features.

## Roadmap

Planned areas:

- Save and load local CV projects
- Template-based PDF export improvements
- More import formats such as DOCX or TXT
- Static CV quality hints
- Optional AI-assisted import and recommendations
- Installer/package builds for supported platforms

## Tech Stack

- .NET 10
- Avalonia UI
- Material.Avalonia
- PdfPig for local PDF text extraction
- xUnit for tests
- markdownlint and C# build checks

## Development

### Prerequisites

- .NET 10 SDK
- Node.js and npm for markdown/C# lint orchestration

### Build

```bash
./scripts/build.sh
```

### Run

```bash
./scripts/run.sh
```

### Test

```bash
./scripts/test.sh
```

### Lint

```bash
npm run lint
```

### Format CSharp

```bash
./scripts/format-cs.sh
```

## Repository Map

```text
src/
  ReVitae/          Avalonia desktop UI
  ReVitae.Core/     CV models, validation, import, localization

tests/
  ReVitae.Tests/    Unit and parser tests

prompts/
  Implementation prompts and product increments

docs/
  Product concept and planning notes
```

## Design Principles

- Keep user data local by default.
- Keep content separate from presentation.
- Make imported content editable immediately.
- Prefer deterministic behavior before AI.
- Add tests for edge cases, not only happy paths.

## License

This project currently uses the license declared in `package.json`.
