# Prompt 003 - Field Validation Schemas

Add validation infrastructure for the main CV form fields.

## Goal

Create a C# validation layer for form fields, similar in purpose to JavaScript
Zod schemas. The goal is to define field validation rules in one place, inspect
them from the UI, and show clear validation feedback to the user.

This step should focus on the `Main / Personal information` section from Prompt 002.
Every field that currently exists in the app must be covered by the validation
schema. Do not leave any existing form field outside the schema.

## Validation Approach

Create a small typed validation system in C#.

The system should allow each field to define:

- field key,
- display label,
- whether the field is required,
- maximum length,
- expected format,
- validation error messages.

The validation definitions should be inspectable from code. For example, the UI
or future tooling should be able to read the schema and understand which fields
exist and which rules apply to them.

## Fields to Validate

Add validation rules for these fields:

- first name,
- last name,
- professional title,
- email,
- phone,
- location,
- LinkedIn URL,
- portfolio or website URL,
- GitHub URL,
- short summary.

## Suggested Rules

Initial validation rules:

- first name: required, maximum 80 characters,
- last name: required, maximum 80 characters,
- professional title: optional, maximum 120 characters,
- email: required, valid email format, maximum 160 characters,
- phone: optional, maximum 40 characters,
- location: optional, maximum 120 characters,
- LinkedIn URL: optional, valid URL, maximum 240 characters,
- portfolio or website URL: optional, valid URL, maximum 240 characters,
- GitHub URL: optional, valid URL, maximum 240 characters,
- short summary: optional, maximum 800 characters.

## UI Behavior

The app should validate fields when their values change.

Validation errors should be visible near the related field or in a simple
validation summary. The user should not need to export a PDF to find out that a
field is invalid.

The PDF export action should be blocked when required fields are missing or any
field is invalid.

## Unit Tests

Add unit tests for the validation schema and validator behavior as part of this
prompt.

The tests should cover normal valid values and important edge cases for every
current field.

Test coverage should include:

- required field validation for first name, last name, and email,
- optional fields accepting empty values,
- maximum length boundaries,
- values exactly at the maximum allowed length,
- values over the maximum allowed length,
- valid and invalid email values,
- valid and invalid URL values,
- whitespace-only required values,
- summary text at and over the maximum length.

These tests should run through the existing C# lint/check flow or be added to it
so validation regressions are caught before commits.

## Out of Scope

Do not implement these features yet:

- server-side validation,
- AI-based validation,
- CV quality scoring,
- import validation,
- template-specific validation,
- multi-language validation messages,
- advanced phone number parsing by country.

## Expected Result

The result should be a reusable C# validation foundation for ReVitae form fields.
It should keep validation rules separate from UI code and make the current main
section safer to edit and export.

All current form fields should be represented in the schema, and the validation
rules should have edge-case unit test coverage.
