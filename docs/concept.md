# ReVitae - Application Concept

## Overview

ReVitae is a desktop application for Windows, macOS, and Linux that helps users create, edit, preview, and export professional CVs.

The application combines a structured CV form, a wide collection of HTML CV templates, live preview, PDF export, and optional AI assistance. The main goal is to let users focus on the content of their CV instead of manually handling layout, formatting, and repetitive editing.

User data should be treated as the source of truth. Templates should only define presentation. This allows the same CV data to be rendered through many different visual styles without losing or duplicating content.

## Product Principles

ReVitae should be practical, clear, and privacy-conscious.

The application should:

- run on Windows, macOS, and Linux,
- provide an installer or packaged binary for each supported system,
- store user CV data locally by default,
- separate CV data from visual templates,
- allow the user to manually edit all generated or imported content,
- never silently download large AI models without user approval,
- clearly explain when AI is used and what data it processes,
- support both users who already have a CV and users who are starting from scratch.

## Phase 1 - Core CV Builder

The first phase should focus on the core product without making local AI model management a blocker.

Phase 1 should deliver a usable CV builder with structured data entry, template selection, preview, and PDF export.

### Structured CV Form

The application should provide an editable form that stores all CV information in a structured format.

Initial form sections should include:

- personal information,
- contact information,
- professional summary,
- work experience,
- education,
- skills,
- languages,
- certificates,
- projects,
- links,
- additional information.

The form should be the main source of truth for the CV. Users should be able to edit every field manually at any time.

### Create a New CV From Scratch

Users should be able to create a completely new CV without importing an existing document.

The application should guide the user through the form and provide default hints about what information is usually useful in a strong CV.

Examples of built-in hints:

- missing sections that may be worth adding,
- overly generic descriptions,
- missing measurable results,
- unclear or inconsistent wording,
- duplicated or unnecessary content,
- work experience that could be explained more clearly.

In Phase 1, these hints can be implemented as static rules or predefined guidance. They do not need to depend on an AI model yet.

### HTML CV Templates

The application should include a broad selection of HTML-based CV templates.

Users should be able to:

- choose a template,
- preview the CV,
- switch to another template without losing data,
- keep the same CV content while changing only the visual style.

Templates should be separated from user data. A template should receive structured CV data and render it into a printable layout.

### Preview and PDF Export

The main output of the application should be a polished PDF CV.

Basic workflow:

1. The user enters or edits CV data in the form.
2. The user selects a CV template.
3. The application renders a preview.
4. The user adjusts content or changes the template.
5. The user exports the final CV to PDF.

Future export formats may include:

- HTML,
- DOCX,
- structured JSON,
- multiple language versions of the same CV.

## Phase 2 - AI Assistance and Model Management

The second phase should add AI-powered features after the core CV builder is stable.

This phase focuses on importing existing CVs, extracting structured information, providing smarter recommendations, and supporting both local and online AI models.

### First Launch AI Setup

On first launch, the application should detect the user's operating system and relevant hardware capabilities.

Based on this detection, it should offer a list of suitable free local AI models for the user's system. The user should also be able to choose an online AI provider instead.

The user should have two main options:

- use a local free AI model,
- connect to a supported online AI model.

If the user chooses a local model, the application should show:

- model name,
- approximate download size,
- expected hardware requirements,
- why the model is recommended,
- where it will be stored.

Only after confirmation should the application download, initialize, and use the selected model.

If the user chooses an online model, the application should provide a setup flow for authentication, such as an API key or another supported connection method.

### Changing the AI Model

The application settings should allow the user to change the selected AI model.

When changing from one local model to another, the application should warn the user that the old local model will be removed and the new one will be downloaded and initialized.

Basic model change flow:

1. The user opens settings.
2. The user selects a different local or online model.
3. The application explains the consequences of the change.
4. If a local model is selected, the old local model is removed.
5. The new model is downloaded.
6. The new model is initialized.
7. Future AI features use the new model.

### Import an Existing CV

Users should be able to upload an existing CV document.

The application should convert the document into text and use AI to extract relevant information into the structured CV form. The extracted data should always be editable before it is used for preview or export.

Initial import formats should include:

- PDF,
- DOCX,
- TXT or another simple text-based format.

### AI Recommendations

AI should act as an assistant, not as an automatic author.

It may suggest improvements such as:

- missing sections,
- weak or generic descriptions,
- places where measurable results could be added,
- unclear wording,
- inconsistent tone,
- duplicated information,
- content that does not fit the selected CV style.

The user must stay in control. AI suggestions should be presented as recommendations that the user can accept, edit, or ignore.

## Open Questions

- Which local AI models should be supported first?
- Should the first AI version support multiple model sizes based on hardware?
- Which online AI providers should be supported?
- Should users be able to add custom HTML templates?
- Should PDF export use an internal renderer or an HTML-to-PDF engine?
- Should the application store each CV as a local project?
- Should CV version history be supported?
- Should the first version support one language or multiple languages?
