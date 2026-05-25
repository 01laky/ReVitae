# ReVitae native project JSON (`*.revitae.json`)

The desktop importer recognizes **ReVitae native JSON** when:

1. The filename ends with `.revitae.json` (structure is **not** sniffed first —
   malformed files still route to the native importer and fail later), **or**
2. Any `.json` file parses to an object whose root contains `"revitaeVersion"`
   alongside structured CV sections.

Mapping is implemented by `ReVitaeJsonMapper` in `ReVitae.Core`.

## Supported revision

**`revitaeVersion: 1`** and **`revitaeVersion: 2`** are accepted.

- **Version 1:** text personal fields only (legacy interchange).
- **Version 2:** same as v1 plus optional embedded profile photo in
  `personalInformation`. Export emits v2 only when a stored photo is present.

Other integers yield `TranslationKeys.ImportErrorUnsupportedStructuredFormat`.

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

| Field                                      | Notes                        |
| ------------------------------------------ | ---------------------------- |
| `firstName`, `lastName`                    | Split-name friendly          |
| `professionalTitle`                        | Job headline                 |
| `email`, `phone`, `location`               | Contact                      |
| `linkedInUrl`, `portfolioUrl`, `gitHubUrl` | Known URL slots              |
| `shortSummary`                             | Profile blurb                |
| `profilePhotoBase64`                       | v2 only — base64 image bytes |
| `profilePhotoContentType`                  | v2 only — MIME type          |

Absolute filesystem paths (`profilePhotoPath`) are **never** serialized in export.
Import writes decoded bytes to local app storage and sets runtime path only in memory.

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

Version **2** adds optional embedded photo fields under `personalInformation`
(export emits v2 only when a stored photo exists):

```json
{
  "revitaeVersion": 2,
  "personalInformation": {
    "firstName": "Jane",
    "lastName": "Doe",
    "email": "jane.doe@example.com",
    "profilePhotoBase64": "<base64-encoded image bytes>",
    "profilePhotoContentType": "image/jpeg"
  }
}
```

On import, decoded bytes are normalized (EXIF orientation for JPEG) and written to
`%LocalAppData%/ReVitae/profile-photos/`. The runtime form holds a local path only;
that path is never written back into exported JSON/YAML.

## Validation rule

At least **one importable datum** must survive deserialization — identical rule
to other structured imports (`CvStructuredImportMapper.HasImportableCvData`).
Empty shells fail with `TranslationKeys.ImportErrorNoStructuredData`.

## File naming convention

Prefer **`your-name.revitae.json`** so detectors classify the file without
reading multi‑megabyte payloads twice.

## Project save/load

The desktop app saves editable CV **projects** to the same native JSON interchange
format. **Save** / **Save As** / **Open** use `CvProjectSerializer` in
`ReVitae.Core/Projects/`.

### Interchange vs project file

| Context                         | `projectSettings` block              |
| ------------------------------- | ------------------------------------ |
| Export modal → **ReVitae JSON** | **Omitted** (interchange-safe)       |
| Toolbar **Save** / **Save As**  | **Included** (editor state)          |
| **Open** project                | Reads CV payload + optional settings |

Generic import (`ReVitaeJsonMapper`) ignores unknown root keys; only the project
loader reads `projectSettings`.

### Optional `projectSettings`

```json
{
  "projectSettings": {
    "schemaVersion": 1,
    "selectedTemplateId": "modernSidebar",
    "dismissedQualityHintKeys": ["work.generic-description||description"],
    "sectionExpandState": {
      "personalInformation": true,
      "workExperience": true,
      "education": false
    },
    "savedAtUtc": "2026-05-25T12:00:00Z",
    "applicationVersion": "0.2.0"
  }
}
```

| Field                      | Notes                                                |
| -------------------------- | ---------------------------------------------------- |
| `schemaVersion`            | Editor-metadata version (currently **1**)            |
| `selectedTemplateId`       | camelCase `CvExportTemplateId` string                |
| `dismissedQualityHintKeys` | Stable keys from `CvQualityAnalyzer.BuildDismissKey` |
| `sectionExpandState`       | Section id → expanded bool                           |
| `savedAtUtc`               | ISO-8601 UTC timestamp on save                       |
| `applicationVersion`       | ReVitae app version string                           |

Files exported for interchange without this block still open as projects; template
and dismissals reset to defaults.

### App data paths

| Path                                                    | Purpose                  |
| ------------------------------------------------------- | ------------------------ |
| `%LocalAppData%/ReVitae/recent-projects.json`           | MRU list (max 8)         |
| `%LocalAppData%/ReVitae/autosave.recovery.revitae.json` | Single recovery autosave |

Recovery is written at most every **60 s** while the document is dirty; deleted
after a successful Save or Open.
