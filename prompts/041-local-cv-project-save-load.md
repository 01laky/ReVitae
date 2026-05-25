# Prompt 041 - Local CV Project Save / Load

Add **durable local project persistence** so users can save their in-progress CV,
reopen it later, and resume editing without re-importing or retyping. The on-disk
format reuses the existing **ReVitae native JSON** interchange (`*.revitae.json`)
already implemented for export/import (prompts **021**, **022**, **023**).

Builds on prompts **002–016** (structured form), **017** (intro modal), **018**
(validation), **019** (template selection), **023** (profile photo v2 embed),
**034** (quality hints — session dismiss → persist in project), and **031** (image
export options may read remembered defaults once a project path exists).

Does **not** implement cloud sync, multi-user collaboration, encrypted project
vaults, or version history timelines.

## Goal

Users can:

1. **Save** the current CV to a `.revitae.json` file (first save opens **Save As**).
2. **Save As** to a new path.
3. **Open** an existing project from disk.
4. See **recent projects** (path + display name) and reopen quickly.
5. Get a **dirty-state indicator** (window title `*` / localized “unsaved changes”).
6. Be prompted before **losing unsaved work** (New CV, Open, Import replace, app close).
7. Optionally **recover an autosave** after an unexpected exit (v1: single recovery file).

Round-trip fidelity for CV **content** must match export → import via
`CvStructuredExportWriter` + `ReVitaeJsonMapper` (including embedded profile photo
v2). Editor-only metadata (template, dismissed hints) lives in an optional
`projectSettings` block that interchange export may omit.

## Priority

**Highest remaining Phase 1 UX gap.** Users currently lose all work when the app
closes unless they manually export `.revitae.json` and re-import on next launch.

## Non-Goals (This Prompt)

- Cloud backup, account login, or shared drives integration,
- Git / diff / merge for project files,
- Multiple open projects or tabbed documents,
- Autosave to arbitrary user-chosen folders every N seconds (recovery file only),
- Persisting import-confidence yellow borders across sessions,
- Persisting AI download state inside the project file (stays in `ai-settings.json`),
- `.revitae` package / ZIP project bundles,
- macOS Quick Look / Windows Jump List shell integrations (optional follow-up),
- Remember-last **image export** options (defer to follow-up after project path exists),
- Changing validation rules or export formats.

## Current State (Before This Prompt)

| Area                 | Today                                                                         |
| -------------------- | ----------------------------------------------------------------------------- |
| Interchange          | `CvStructuredExportWriter.WriteRevitaeJson` + `ReVitaeJsonMapper.Map`         |
| UI snapshot          | `MainWindow.BuildExportSourceData()` → `CvExportSourceDataFactory`            |
| Hydration            | `ApplyCvImportResult(CvImportResult)` populates all sections                  |
| New / replace guards | `HasCvFormData()` + confirm modals for New CV / Import                        |
| Template             | `_selectedTemplate` in memory only (`CvExportTemplateId`)                     |
| Quality dismiss      | `QualityHintDismissalStore` — session-only, cleared on New CV / import        |
| App data dir         | `ReVitaeLocalDataPaths.GetReVitaeRootDirectory()` (`%LocalAppData%/ReVitae/`) |
| Window close         | No unsaved-changes handler                                                    |
| Intro modal          | Create new / Import only — no Open recent                                     |

## Product Behavior Summary

### Primary actions (header toolbar)

Add three compact actions near existing header controls (icon + tooltip; match
Material icon buttons used for Setup / Templates / About):

| Action      | Behavior                                                                    |
| ----------- | --------------------------------------------------------------------------- |
| **Save**    | If no path → **Save As**. Else atomic write to current path; clear dirty.   |
| **Save As** | Native save dialog (`*.revitae.json`); update current path; add to recents. |
| **Open**    | Native open dialog; if dirty → confirm discard/save/cancel; load file.      |

Keyboard shortcuts (platform-aware):

- **Save** — `Ctrl+S` / `Cmd+S`
- **Open** — `Ctrl+O` / `Cmd+O`

Disable Save/Open while blocking overlays are open (intro, import progress, export
modal, AI modals) — same guard pattern as `IsBlockingOverlayOpen()`.

### Window title

When a project path is bound:

```text
Jane_Doe_CV.revitae.json — ReVitae
```

When dirty:

```text
* Jane_Doe_CV.revitae.json — ReVitae
```

When unsaved new document (no path yet) with data:

```text
* Untitled CV — ReVitae
```

Use localized “Untitled CV” key; do not hard-code English in title logic.

### Dirty tracking

Mark dirty when **any** of these change after last successful save/load:

- personal fields or profile photo path,
- repeatable section entries (add/edit/remove/reorder if reorder exists),
- additional information text,
- selected template id,
- dismissed quality hint keys.

Do **not** mark dirty for preview-only refreshes, validation recalculation, or
quality hint analysis without dismissals.

Implementation: lightweight `CvProjectDirtyTracker` fed by existing section change
events / `TextChanged` handlers already wired for validation — avoid polling the
whole form on a timer.

### Unsaved-changes confirmation

Reuse the existing in-window confirm modal pattern (`NewCvConfirm*`, `ReplaceCvConfirm*`).

New modal **`UnsavedChangesConfirm*`** with three actions:

| Button      | Action                                                                    |
| ----------- | ------------------------------------------------------------------------- |
| **Save**    | Run Save (or Save As if no path); continue pending action only on success |
| **Discard** | Proceed without saving                                                    |
| **Cancel**  | Abort pending action                                                      |

Trigger before:

- **New CV** (header + intro paths),
- **Open** project,
- **Import** replace (header upload),
- **Window close** (`Closing` event — set `e.Cancel = true` when user cancels).

If the form is empty and not dirty, skip the modal.

### Intro modal (startup)

Extend intro modal with a third primary path:

1. **Create new CV** — unchanged.
2. **Import existing CV** — unchanged.
3. **Open saved project** — file picker → load → close intro.

Below the buttons, when recents exist, show **Recent projects** list (max **8** entries):

- display name = filename without extension (or `FirstName LastName` when personal
  fields are present in cached metadata),
- subtitle = shortened absolute path + relative “last opened” time,
- click → load project (dirty guard skipped on cold start),
- “Clear recent list” text button (confirm once).

**Optional v1 (recommended):** on startup, if `%LocalAppData%/ReVitae/autosave.recovery.revitae.json`
exists and is newer than 0 bytes, show a **Recover unsaved work?** banner on the intro
modal (Recover / Discard recovery file). Do not auto-load silently.

### Save dialog defaults

Reuse export naming via `CvExportFilenameHelper.SuggestFilename(first, last, RevitaeJson)`
when personal names exist; else `Untitled_CV.revitae.json`.

File picker filter:

- pattern `*.revitae.json` primary,
- optional secondary `*.json` with helper text that ReVitae native JSON is preferred.

Title keys: `ProjectSaveDialogTitle`, `ProjectOpenDialogTitle`.

### Success / error feedback

- Success: Material snackbar — `ProjectSaved` / `ProjectOpened` (include filename).
- Failure: snackbar or inline export-status area with localized keys:
  `ProjectSaveFailed`, `ProjectOpenFailed`, `ProjectOpenUnsupportedVersion`,
  `ProjectOpenEmpty`.

Never leave the UI in a half-hydrated state on failed open — keep prior document.

## On-Disk Project Format

### Principle

**CV payload** stays compatible with existing `revitaeVersion` **1** and **2** rules
documented in [`docs/revitae-project-json.md`](../docs/revitae-project-json.md).

**Editor metadata** is an optional sibling root object ignored by generic interchange
import unless explicitly loaded as a project.

### Root shape (project file)

```json
{
  "revitaeVersion": 2,
  "personalInformation": {},
  "workExperience": [],
  "education": [],
  "skills": [],
  "languages": [],
  "certificates": [],
  "projects": [],
  "links": [],
  "additionalInformation": { "content": "" },
  "projectSettings": {
    "schemaVersion": 1,
    "selectedTemplateId": "modernSidebar",
    "dismissedQualityHintKeys": ["work.generic-description"],
    "sectionExpandState": {
      "personalInformation": true,
      "workExperience": true,
      "education": false
    },
    "savedAtUtc": "2026-05-25T12:00:00Z",
    "applicationVersion": "0.1.0"
  }
}
```

### `projectSettings` rules

| Field                      | Required | Notes                                                                      |
| -------------------------- | -------- | -------------------------------------------------------------------------- |
| `schemaVersion`            | yes      | Start at **1**; bump only for breaking editor-metadata changes             |
| `selectedTemplateId`       | no       | camelCase enum name string matching `CvExportTemplateId` (`modernSidebar`) |
| `dismissedQualityHintKeys` | no       | Stable ids from `CvQualityAnalyzer.BuildDismissKey`                        |
| `sectionExpandState`       | no       | Map section id → bool; omit unknown sections                               |
| `savedAtUtc`               | no       | ISO-8601 UTC set on each save                                              |
| `applicationVersion`       | no       | From `Version.props` / assembly informational version                      |

Serialization:

- camelCase JSON, indented, UTF-8 without BOM,
- **atomic write**: temp file + rename (same pattern as `AiDownloadJobStorage`,
  `AiSettingsRepository`).

Import / load behavior:

- Unknown `projectSettings.schemaVersion` **newer** than supported → load CV payload
  anyway; ignore unsupported settings; show non-blocking `ProjectSettingsPartiallyIgnored`
  snackbar once.
- Malformed `projectSettings` → ignore block; load CV payload; log via
  `CvImportDiagnosticsLogger` when `REVITAE_IMPORT_DEBUG=1`.
- Files **without** `projectSettings` (plain interchange export) still open as projects;
  apply default template + empty dismiss store + import-style section expand rules.

### Interchange export unchanged

Export modal → **ReVitae JSON** continues to write **interchange-safe** JSON:

- include `revitaeVersion` + CV sections only,
- **omit** `projectSettings` unless user explicitly chooses “Include editor settings”
  — **default omit** to keep prompt **022** semantics.

Save / Save As from the new toolbar **always** includes `projectSettings` when any
editor setting differs from defaults (or always include the block for simplicity — prefer
**always include** for deterministic round-trip of template + dismissals).

### Autosave recovery file

Path: `ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath()` →
`%LocalAppData%/ReVitae/autosave.recovery.revitae.json`

- Write at most every **60 s** while dirty (debounced, atomic),
- Delete on clean Save or successful Open of any project,
- Same schema as user projects,
- Never overwrite the user’s explicit Save path automatically.

## Architecture

```text
MainWindow (partials: MainWindow.Projects.cs)
        │
        ├─ CvProjectSession (UI) — current path, dirty, display name
        │
        ▼
┌───────────────────────────────┐
│ CvProjectService (Core)        │
│  Save / Load / TryLoadRecovery │
└───────────────────────────────┘
        │
        ├─ CvProjectSerializer — JSON compose / parse
        │     ├─ CvStructuredExportWriter (reuse BuildRevitaeDto)
        │     └─ ReVitaeJsonMapper (reuse Map → CvImportResult)
        │
        ├─ RecentProjectsStore — recent-projects.json (atomic)
        └─ ReVitaeLocalDataPaths — autosave path helpers
```

**Keep persistence in Core; keep Avalonia pickers in UI** — mirror export/import split.

### Core types (`ReVitae.Core/Projects/`)

```csharp
public sealed record CvProjectSettings(
    int SchemaVersion,
    CvExportTemplateId? SelectedTemplateId,
    IReadOnlyList<string> DismissedQualityHintKeys,
    IReadOnlyDictionary<string, bool>? SectionExpandState,
    DateTimeOffset? SavedAtUtc,
    string? ApplicationVersion);

public sealed record CvProjectSaveRequest(
    CvExportSourceData Source,
    CvProjectSettings Settings);

public sealed record CvProjectLoadResult(
    bool Success,
    CvImportResult? Import,
    CvProjectSettings? Settings,
    string? ErrorMessageKey);

public static class CvProjectSerializer { ... }
public static class CvProjectService { ... }
public sealed class RecentProjectsStore { ... }
public sealed record RecentProjectEntry(
    string Path,
    string DisplayName,
    DateTimeOffset LastOpenedUtc);
```

`CvProjectSerializer.Save`:

1. Build CV DTO via shared helper extracted from `CvStructuredExportWriter` (refactor
   minimal internal method if needed — do not duplicate field mapping).
2. Attach `projectSettings`.
3. Serialize with shared `JsonSerializerOptions` (camelCase, indented).
4. Write atomically to target path.

`CvProjectSerializer.Load`:

1. Read text; size guard **25 MB** (`CvImportLimits.MaxBytes` — reuse constant).
2. Parse root; delegate CV sections to `ReVitaeJsonMapper.Map(json)` (full string is fine).
3. Parse `projectSettings` separately with forward-compatible DTO.
4. Return `CvProjectLoadResult`.

Template id parsing: case-insensitive enum parse; unknown id →
`CvExportTemplateId.CleanTopHeader` default + diagnostic warning key.

### UI session (`MainWindow.Projects.cs`)

```csharp
private CvProjectSession _projectSession = CvProjectSession.Empty;
```

Responsibilities:

- wire Save / Save As / Open handlers,
- update window title on dirty/path changes,
- call `ApplyCvImportResult` + apply settings (template, dismiss store, expand state),
- integrate unsaved confirm modal,
- hook `Closing` event,
- debounced autosave timer (DispatcherTimer or existing app timer pattern).

After successful load:

- set `_projectSession` path,
- clear dirty,
- `ResetQualityHintState()` then re-apply dismissed keys from settings,
- `SelectTemplate(settings.SelectedTemplateId ?? default)`,
- `UpdatePreview()`, `UpdateValidationState()`, `UpdateQualityHints()`,
- close intro modal if open.

After successful save:

- update recents,
- delete recovery autosave,
- clear dirty,
- snackbar.

### Recent projects store

File: `%LocalAppData%/ReVitae/recent-projects.json`

```json
{
  "entries": [
    {
      "path": "/Users/jane/Documents/Jane_Doe_CV.revitae.json",
      "displayName": "Jane_Doe_CV",
      "lastOpenedUtc": "2026-05-25T11:00:00Z"
    }
  ]
}
```

Rules:

- max **8** entries, MRU order,
- skip missing files on read (prune lazily),
- store **absolute** paths,
- dedupe by normalized path (case-insensitive on Windows).

## Integration Points

### Reuse import hydration

**Do not** duplicate form mapping. Successful load → `ApplyCvImportResult(result)` +
settings overlay.

Clear import-confidence state on project open (`_lastImportConfidences = []`) — yellow
borders are import-session only.

### New CV / Import replace

After `ClearCvForm()` / successful import replace:

- reset `_projectSession` to empty (no path),
- mark dirty appropriately (import → dirty true; new empty → dirty false).

### Export validation gate

Saving a project **does not** require export validation to pass (same as interchange
JSON export policy — user may save drafts with validation errors). Optional info
snackbar when saving with errors: `ProjectSavedWithValidationErrors` (non-blocking).

### Profile photo

Save embeds photo as v2 base64 when present (existing writer logic). Load uses existing
`ReVitaeJsonMapper` photo decode → `ProfilePhotoStorage` path assignment.

## Localization

Add EN + SK keys (`TranslationKeys` + `AppLocalizer`); register in **RequiredKeys**:

| Constant                           | Key string                          | English (example)                                      |
| ---------------------------------- | ----------------------------------- | ------------------------------------------------------ |
| `ProjectSave`                      | `project.save`                      | Save                                                   |
| `ProjectSaveAs`                    | `project.saveAs`                    | Save As                                                |
| `ProjectOpen`                      | `project.open`                      | Open                                                   |
| `ProjectSaveDialogTitle`           | `project.saveDialogTitle`           | Save CV project                                        |
| `ProjectOpenDialogTitle`           | `project.openDialogTitle`           | Open CV project                                        |
| `ProjectSaved`                     | `project.saved`                     | Saved {0}                                              |
| `ProjectOpened`                    | `project.opened`                    | Opened {0}                                             |
| `ProjectSaveFailed`                | `project.saveFailed`                | Could not save the project.                            |
| `ProjectOpenFailed`                | `project.openFailed`                | Could not open the project.                            |
| `ProjectUntitled`                  | `project.untitled`                  | Untitled CV                                            |
| `ProjectUnsavedIndicator`          | `project.unsavedIndicator`          | Unsaved changes                                        |
| `ProjectUnsavedConfirmTitle`       | `project.unsavedConfirmTitle`       | Save changes?                                          |
| `ProjectUnsavedConfirmMessage`     | `project.unsavedConfirmMessage`     | Your CV has unsaved changes. Save before continuing?   |
| `ProjectUnsavedSave`               | `project.unsavedSave`               | Save                                                   |
| `ProjectUnsavedDiscard`            | `project.unsavedDiscard`            | Don't save                                             |
| `ProjectRecentTitle`               | `project.recentTitle`               | Recent projects                                        |
| `ProjectRecentClear`               | `project.recentClear`               | Clear list                                             |
| `ProjectRecentMissing`             | `project.recentMissing`             | File no longer exists                                  |
| `ProjectRecoveryTitle`             | `project.recoveryTitle`             | Recover unsaved work?                                  |
| `ProjectRecoveryMessage`           | `project.recoveryMessage`           | ReVitae found an autosaved CV from an unexpected exit. |
| `ProjectRecoveryRestore`           | `project.recoveryRestore`           | Recover                                                |
| `ProjectRecoveryDiscard`           | `project.recoveryDiscard`           | Discard                                                |
| `ProjectSettingsPartiallyIgnored`  | `project.settingsPartiallyIgnored`  | Some editor settings were ignored (newer format).      |
| `ProjectSavedWithValidationErrors` | `project.savedWithValidationErrors` | Project saved — fix validation errors before export.   |
| `IntroOpenProject`                 | `intro.openProject`                 | Open saved project                                     |
| `ProjectFileType`                  | `project.fileType`                  | ReVitae project                                        |

Tooltips for header buttons reuse action labels.

## Testing

### Core unit tests (`tests/ReVitae.Tests/Projects/`)

| Test area                  | Cases                                                                             |
| -------------------------- | --------------------------------------------------------------------------------- |
| `CvProjectSerializerTests` | round-trip CV v1/v2 + settings; omit settings block; unknown template id fallback |
|                            | atomic save creates valid JSON; 25 MB guard rejects oversized read                |
|                            | dismissed hint keys preserved; section expand map preserved                       |
| `RecentProjectsStoreTests` | MRU order, cap 8, dedupe, prune missing path                                      |
| `CvProjectServiceTests`    | save then load identity; recovery write/delete lifecycle                          |

Use `JohnDoeStressCvDataset` or minimal architect fixture for rich payloads; assert
zero validation errors after load when fixture is complete.

### Integration tests

- Export Revitae JSON (interchange, no settings) → open as project → template defaults.
- Save project with `ModernSidebar` + one dismissed hint → reload → assert template +
  dismiss store contents (via analyzer filtered hints count).

### UI / manual QA checklist

1. Edit CV → `*` title → Save → title clean.
2. Save As → new path → recent list updated.
3. Open → dirty confirm → Save branch / Discard branch / Cancel branch.
4. New CV with dirty form → confirm.
5. Import replace with dirty form → confirm.
6. Close window with dirty form → confirm; Cancel keeps app open.
7. Intro → Open project → form populated, intro closes.
8. Recent entry missing on disk → graceful message, entry removed.
9. Kill app during edit → restart → recovery banner → Recover restores content.
10. SK locale smoke on new strings.

## Documentation Updates

| File                                                                   | Change                                                                             |
| ---------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| [`docs/revitae-project-json.md`](../docs/revitae-project-json.md)      | Document optional `projectSettings` block + autosave recovery path                 |
| [`docs/concept.md`](../docs/concept.md)                                | Move save/load from “Still open” to implemented after prompt lands                 |
| [`README.md`](../README.md)                                            | Highlight Save/Open/Recent; update roadmap; mermaid optional branch “Save project” |
| [`CHANGELOG.md`](../CHANGELOG.md)                                      | Unreleased entry with test count delta                                             |
| [`prompts/034-cv-quality-hints.md`](034-cv-quality-hints.md)           | Cross-link: dismiss persistence via **041**                                        |
| [`prompts/031-image-export.md`](031-image-export.md)                   | Cross-link: remember-last image options follow-up after **041**                    |
| [`prompts/040-ai-assisted-cv-import.md`](040-ai-assisted-cv-import.md) | Cross-link: dirty guard on Enhance replace; `ApplyCvImportResult` hydration        |

## Out of Scope (Follow-Ups)

- per-project export preferences (image options, last used export format),
- “Pin” favorite projects,
- macOS document model / single-instance open file event,
- encrypting project files at rest,
- incremental / append-only autosave history.

## Validation (Definition of Done)

- `./scripts/test.sh` passes; add **25+** focused project tests.
- `npm run lint` passes (MD table alignment).
- `./scripts/format-cs.sh` clean.
- Manual QA checklist above completed.
- Export modal ReVitae JSON (interchange) still omits `projectSettings` by default.
- No regression to import replace, New CV, export validation gate, or AI modals.

## Expected Result

ReVitae behaves like a desktop document editor: users **save and reopen** CV projects
as `.revitae.json` files with template and quality-hint dismissals restored, see
**recent projects** at startup, and are protected from accidental data loss via dirty
tracking and confirmation modals — built on the existing structured JSON interchange
without cloud dependencies.
