# ReVitae native project JSON (`*.revitae.json`)

The desktop importer recognizes **ReVitae native JSON** when:

1. The filename ends with `.revitae.json` (structure is **not** sniffed first —
   malformed files still route to the native importer and fail later), **or**
2. Any `.json` file parses to an object whose root contains `"revitaeVersion"`
   alongside structured CV sections.

Mapping is implemented by `ReVitaeJsonMapper` in `ReVitae.Core`.

## Supported revision

Only **`revitaeVersion: 1`** is accepted today. Other integers yield
`TranslationKeys.ImportErrorUnsupportedStructuredFormat`.

## Root shape

Camel‑case properties mirror serialized CV models (`JsonSerializer` defaults in
code). Minimal conceptual schema:

```json
{
  "revitaeVersion": 1,
  "personalInformation": {},
  "workExperience": [],
  "education": [],
  "skills": [],
  "languages": [],
  "certificates": [],
  "projects": [],
  "links": [],
  "additionalInformation": {
    "content": ""
  }
}
```

### `personalInformation`

Fields align with `PersonalInformationImport`:

| Field                                      | Notes               |
| ------------------------------------------ | ------------------- |
| `firstName`, `lastName`                    | Split-name friendly |
| `professionalTitle`                        | Job headline        |
| `email`, `phone`, `location`               | Contact             |
| `linkedInUrl`, `portfolioUrl`, `gitHubUrl` | Known URL slots     |
| `shortSummary`                             | Profile blurb       |

### Repeatable sections

Arrays deserialize directly into core CV entries:

- `workExperience` → `WorkExperienceEntry`
- `education` → `EducationEntry`
- `skills` → grouped `{ category, skills: [{ name }] }` snapshots
- `languages` → `LanguageEntry`
- `certificates` → `CertificateEntry`
- `projects` → `ProjectEntry`
- `links` → `LinkEntry`

Exact property names match export-ready models (months/years integers,
technology arrays on projects, etc.).

### `additionalInformation`

Optional wrapper object with string `content`. Stored as normalized additional
information text after import.

## Example

```json
{
  "revitaeVersion": 1,
  "personalInformation": {
    "firstName": "Jane",
    "lastName": "Doe",
    "professionalTitle": "Platform Engineer",
    "email": "jane.doe@example.com",
    "phone": "+421900000000",
    "location": "Bratislava, Slovakia",
    "shortSummary": "Backend-focused engineer shipping observability tooling."
  },
  "skills": [
    {
      "category": "Languages",
      "skills": [{ "name": "C#" }, { "name": "Go" }]
    }
  ],
  "workExperience": [],
  "education": [],
  "languages": [],
  "certificates": [],
  "projects": [],
  "links": [],
  "additionalInformation": {
    "content": "Speaks at local meetups."
  }
}
```

## Validation rule

At least **one importable datum** must survive deserialization — identical rule
to other structured imports (`CvStructuredImportMapper.HasImportableCvData`).
Empty shells fail with `TranslationKeys.ImportErrorNoStructuredData`.

## File naming convention

Prefer **`your-name.revitae.json`** so detectors classify the file without
reading multi‑megabyte payloads twice.
