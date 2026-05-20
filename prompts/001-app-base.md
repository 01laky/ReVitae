# Prompt 001 - App Base

Build the first minimal version of ReVitae.

## Goal

Create a very simple desktop app base that allows the user to enter a few basic CV fields and export them into a plain, lightweight PDF.

This first version is not about design, templates, AI, imports, or advanced CV structure. It should only prove the basic flow:

1. User opens the app.
2. User fills a simple form.
3. User previews the entered information.
4. User exports a basic PDF.

## Form Fields

The form should contain only these fields:

- first name,
- last name,
- email.

All fields should be editable.

## Preview

The app should show a simple preview of the entered data.

The preview can be plain text or a very basic document layout. No advanced styling is required.

Example preview content:

```text
First name: John
Last name: Doe
Email: john.doe@example.com
```

## PDF Export

The app should allow the user to export the entered information into a PDF file.

The PDF should be intentionally simple:

- no template system,
- no custom design,
- no colors required,
- no images,
- no AI-generated text,
- only the data from the form.

The PDF should contain the first name, last name, and email in a readable format.

## Out of Scope

Do not implement these features yet:

- AI model selection,
- AI model download,
- online AI providers,
- CV import,
- DOCX/PDF parsing,
- advanced CV sections,
- HTML template system,
- rich design,
- user accounts,
- cloud sync,
- multi-language support.

## Expected Result

The result should be a clean application foundation that can later be extended with:

- more CV fields,
- templates,
- import,
- AI recommendations,
- model management,
- better PDF layout.
