# Prompt 002 - Main Personal Information

Extend the first minimal ReVitae app by expanding the main CV information
section.

## Goal

The goal of this step is to replace the very small three-field form from Prompt
001 with a more realistic `Main / Personal information` section.

This is still not a full CV builder. It should only improve the main identity
and contact section of the CV while keeping the app simple.

## Main Section Fields

The form should contain these editable fields:

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

## Field Notes

`Professional title` is a short headline shown near the user's name.

Examples:

- Frontend Developer,
- Project Manager,
- Data Analyst,
- UX Designer.

`Location` should be a simple city/country value. It should not require a full
street address.

`Short summary` should be a short free-text section, usually two to four
sentences. It represents the user's professional profile.

## Preview

The app should update the preview when any field changes.

The preview should show the main section in a simple readable structure:

```text
John Doe
Frontend Developer

Email: john.doe@example.com
Phone: +421 900 000 000
Location: Bratislava, Slovakia
LinkedIn: https://www.linkedin.com/in/johndoe
Portfolio: https://johndoe.dev
GitHub: https://github.com/johndoe

Summary:
Frontend developer focused on building clean and accessible user interfaces.
```

No advanced layout or visual design is required yet.

## PDF Export

The PDF export should include all fields from the main section.

The PDF should remain plain and lightweight:

- no template system,
- no custom design,
- no images,
- no colors required,
- no AI-generated text,
- only the data from the form.

## Out of Scope

Do not implement these features yet:

- work experience,
- education,
- skills,
- languages,
- certificates,
- projects,
- photo upload,
- AI model selection,
- AI recommendations,
- CV import,
- HTML template system,
- rich design,
- user accounts,
- cloud sync.

## Expected Result

The result should be a better app foundation with a realistic CV header/main
section that can later be extended with additional CV sections.
