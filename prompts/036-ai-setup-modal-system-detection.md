# Prompt 036 - AI Setup Modal with System Detection and Model Recommendation

Add a dedicated **AI** entry point in the header toolbar and an in-window **AI setup
modal**. Each time the user opens the modal, ReVitae runs a short **system detection**
pass (with a visible loader), then recommends one **local AI model** from a curated
catalog the user can download for later Phase 2 features.

This is the **first Phase 2 AI prompt**. It implements the model-selection foundations
from [`docs/concept.md`](../docs/concept.md) without yet calling a model for CV import,
review, or rewriting. Concept describes a **first-launch** wizard; this prompt
deliberately exposes the same flow **on demand** via a header icon (every open re-runs
detection). A dedicated first-launch gate may come in a follow-up prompt.

Builds on prompt **004** (modal shell), **007** (i18n), **020** (About vs Setup split),
and **034** (static quality hints remain separate from AI).

## Goal

The user clicks a new **AI icon** in the top-right header panel. An in-window modal
opens, shows a **loader while the system is analyzed**, then presents:

1. a summary of the detected environment (OS, architecture, approximate RAM),
2. a **recommended local model** highlighted for that hardware,
3. a scrollable list of **all supported downloadable models** from the catalog,
4. per-model metadata (download size, minimum RAM, brief “why / not suitable” copy),
5. actions to **download the selected model** (with explicit confirmation) or close
   without downloading.

**Critical UX rule:** detection runs **every time the modal is opened**, not only on
first launch. Closing and reopening the modal must trigger a fresh detection pass and
loader state (even if results are usually identical).

## Non-Goals (This Prompt)

- Using an AI model for CV import, field rewrite, or quality review (later prompts),
- Online / cloud AI providers and API keys (later prompt **037**),
- Ollama installation bootstrap or bundling Ollama inside ReVitae,
- Silent background downloads,
- Persisting “user dismissed AI forever” across app restarts (optional follow-up),
- Replacing or merging the existing **Setup** modal (language stays there),
- Auto-starting downloaded models as a background service beyond a minimal “pull +
  verify” check,
- Telemetry or sending system profile to the network,
- Removing previously pulled Ollama models when the user picks another (concept
  describes this for settings-driven model change — defer to a later prompt).

## Product Behavior

### Header icon

Add a new icon button to the existing horizontal header panel (`MainWindow.axaml`),
consistent with `OpenSetupButton`, `OpenTemplatesButton`, and `OpenAboutButton`:

- **Icon:** `MaterialIconKind.Robot` (Material Icons Avalonia; do not use unverified
  kind names),
- **Tooltip / automation name:** localized via `TranslationKeys.OpenAiSetup`,
- **Placement:** after **Upload CV** (`UploadCvButton`), before **Setup**
  (`OpenSetupButton`).

Clicking the button opens the AI modal. It must **not** change CV form data, validation,
preview, or quality hints.

**Disable the button** when `IsBlockingOverlayOpen()` is true (intro overlay or replace-CV
import progress), matching other header actions that must not compete with blocking
flows.

### Modal shell

Follow the same in-window overlay pattern as Setup / Templates / Export modals:

- semi-transparent backdrop (`re-vitae-modal-overlay`),
- centered panel (`re-vitae-modal-panel`),
- approximate **80%** width / height of the current window (responsive via existing
  `UpdateModalSizes()`),
- top-right **X** close + bottom **Close** button,
- **Escape** closes the modal when it is topmost (extend `OnWindowKeyDown` stack — place
  `AiSetupModalOverlay` after `ExportModalOverlay` and before `TemplatesModalOverlay`,
  or in the same priority band as other content modals),
- wire `SetAiSetupModalVisible(bool)` to call `HideOtherContentModals(AiSetupModalOverlay)`
  so only one content modal is open at a time (include `QualityHintModalOverlay` in
  `HideOtherContentModals` if not already there when implementing).

Suggested title: localized **AI setup** (distinct from Setup / About).

### Open → detect → recommend flow

Every time the modal becomes visible:

```text
Opening
   │
   ▼
┌─────────────────────────┐
│  Detecting system…      │  ← loader (indeterminate ProgressBar + status text)
│  (blocks interaction    │
│   with model list)      │
└─────────────────────────┘
   │  IAsync detection (target ≤ 2 s on typical hardware)
   ▼
┌─────────────────────────┐
│  System summary         │  e.g. macOS · arm64 · ~16 GB RAM
│  Privacy note           │  local-only detection (see below)
│  ★ Recommended model    │  highlighted card
│  All models (list)      │  scrollable catalog
│  [Download selected]    │
│  [Close]                │
└─────────────────────────┘
```

**Loader requirements:**

- Show immediately on open (no empty flash of stale content — clear/hide content panel
  before starting detection),
- Hide model list and download actions until detection completes,
- Optional minimum loader display (~300 ms) to avoid sub-frame flicker when detection is
  instant in tests,
- If detection fails, show a localized error panel with **Retry** (re-runs detection)
  and **Close**,
- Reopening the modal always restarts at the loader (clear previous recommendation UI).

**Do not** cache detection results across modal sessions in a way that skips the
loader on reopen. In-memory work during a single open is fine; each new open starts
fresh.

**Modal states (implement explicitly):** `Detecting` → `Ready` | `DetectionFailed`;
during pull: `Pulling` → `PullComplete` | `PullFailed`.

### Privacy note (local-only)

Below the system summary (and above the model catalog), show a compact **privacy /
local-only banner** — secondary text style (`re-vitae-secondary`), not dismissible:

- Copy (EN): *System detection runs only on this device. ReVitae does not send your
  hardware profile or CV data to ReVitae servers.*
- Visible in the **Ready** state (hidden during loader and pull progress),
- No checkbox or “I agree” — informational only; aligns with Non-Goals (no telemetry).

SK translation required in `AppLocalizer` alongside EN.

## System Detection (Core)

Implement hardware/OS profiling in **`ReVitae.Core`**, not in Avalonia code-behind.
Ollama HTTP reachability is a **separate probe** (see below) — keep RAM/OS logic
testable without network.

### New namespace: `ReVitae.Core/Ai/`

Suggested types:

```csharp
public enum AiPlatform
{
    Windows = 0,
    MacOS = 1,
    Linux = 2,
    Unknown = 3,
}

public enum AiModelTier
{
    Small = 0,
    Medium = 1,
    Large = 2,
}

public enum AiRuntimeKind
{
    Disabled = 0,
    Ollama = 1,          // v1 local runtime target
}

public sealed record SystemProfile(
    AiPlatform Platform,
    string Architecture,              // arm64, x64, …
    long? TotalPhysicalMemoryBytes,
    int ProcessorCount,
    string? DetectionWarningKey);     // optional i18n key when data is partial

public sealed record OllamaRuntimeStatus(
    bool IsReachable,
    IReadOnlyList<string> InstalledModelTags);  // from GET /api/tags when reachable

public sealed record AiModelCatalogEntry(
    string Id,                        // stable slug, e.g. "small-instruct"
    string DisplayNameKey,            // TranslationKeys.*
    long ApproxDownloadBytes,
    long MinimumMemoryBytes,
    AiModelTier Tier,
    string OllamaModelTag,            // e.g. "llama3.2:3b-instruct"
    IReadOnlyList<AiPlatform> SupportedPlatforms);

public sealed record AiModelRecommendation(
    AiModelCatalogEntry Model,
    bool IsRecommended,
    bool IsDownloadAllowed,           // false when detected RAM < MinimumMemoryBytes
    string? ReasonKey);               // why recommended or "requires more RAM"

public sealed record AiSystemDetectionResult(
    SystemProfile Profile,
    OllamaRuntimeStatus Ollama,
    IReadOnlyList<AiModelRecommendation> Models,
    AiModelCatalogEntry? RecommendedModel);
```

### Detection services

```csharp
public interface ISystemProfileDetector
{
    Task<SystemProfile> DetectAsync(CancellationToken cancellationToken = default);
}

public interface IOllamaRuntimeProbe
{
    Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default);
}

public interface IDiskSpaceChecker
{
    /// <summary>
    /// Returns free bytes on the volume used for ReVitae local data
    /// (same root as ProfilePhotoStorage / ai-settings.json), or null if unknown.
    /// </summary>
    long? GetAvailableBytesForLocalData();

    bool HasSpaceForDownload(long approxDownloadBytes, double bufferFactor = 1.1);
}

public static class AiModelRecommendationService
{
    public static AiSystemDetectionResult Recommend(
        SystemProfile profile,
        OllamaRuntimeStatus ollama,
        IReadOnlyList<AiModelCatalogEntry>? catalog = null);
}
```

UI orchestration (in `MainWindow.AiSetup.cs`) runs **both** probes concurrently during
the loader phase, then calls `Recommend`. Use `CancellationToken` linked to modal lifetime.

**Detection rules (v1, deterministic — no ML):**

| Signal | Source |
| ------ | ------ |
| OS | `RuntimeInformation.IsOSPlatform` → `AiPlatform` |
| Architecture | `RuntimeInformation.ProcessArchitecture` → string (`Arm64`, `X64`, …) |
| RAM | OS-specific best effort (`GlobalMemoryStatusEx` on Windows, `sysctl` on macOS, `/proc/meminfo` on Linux); if unknown, `null` + warning key |
| CPU count | `Environment.ProcessorCount` |
| Ollama running | `IOllamaRuntimeProbe`: `GET http://127.0.0.1:11434/api/tags`, timeout **≤ 1 s** |

Platform-specific RAM readers may live under `ReVitae.Core/Ai/Platform/` (partial classes
or small OS files). Do **not** reference Avalonia from Core.

**Disk space (Core):** implement `IDiskSpaceChecker` with `DriveInfo` on the root of
`ProfilePhotoStorage.GetDefaultStorageDirectory()` (or shared helper for ReVitae local
data root). `HasSpaceForDownload` returns `false` when free bytes are unknown or
`available < (long)(approxDownloadBytes * bufferFactor)`.

**Recommendation heuristic (document in code comments):**

Use catalog `MinimumMemoryBytes` as the hard gate for `IsDownloadAllowed`.

| Detected RAM | Behavior |
| ------------ | -------- |
| `null` (unknown) | Recommend **Small** tier; set `DetectionWarningKey`; mark models with `IsDownloadAllowed = true` only for Small (conservative) |
| `< 8 GB` | Recommend Small if allowed; show banner that even Small needs ~8 GB — user may still attempt at own risk only if you add an explicit override later; **v1: disable download** for all models when RAM &lt; 8 GB |
| `8–15 GB` | Recommend **Small** only (Medium/Large require ≥ 16 GB / 64 GB) |
| `16–63 GB` | Recommend **Medium**; Large visible but `IsDownloadAllowed = false` unless RAM ≥ 64 GB × headroom factor |
| `≥ 64 GB` | Recommend **Medium** by default; **Large** recommended only when RAM ≥ Large `MinimumMemoryBytes` × **1.25** headroom |

Headroom factor prevents recommending a model that fits on paper but leaves no room for
OS + ReVitae + Ollama runtime.

Keep the catalog **small and curated** (3 entries for v1):

| Id | Ollama tag | Approx download | Min RAM |
| -- | ---------- | --------------- | ------- |
| `small-instruct` | `llama3.2:3b-instruct` | ~2 GB | 8 GB |
| `medium-instruct` | `llama3.1:8b-instruct` | ~4.7 GB | 16 GB |
| `large-instruct` | `llama3.1:70b-instruct` | ~40 GB | 64 GB |

Exact tags and sizes are **data in Core** (`AiModelCatalog.Default`) so tests do not
depend on the network. Tags must match [Ollama library](https://ollama.com/library)
names at implementation time.

If `Ollama.InstalledModelTags` already contains the selected tag, show **Already
downloaded** badge and skip pull unless user explicitly chooses **Re-download** (optional
v1: hide Download button when installed).

## Download Flow (Ollama pull)

v1 assumes the user already has **Ollama installed and running** locally. ReVitae does
**not** install Ollama in this prompt.

When the user clicks **Download** on a catalog entry:

1. If `!Ollama.IsReachable`, show localized error + hint link to `https://ollama.com`
   (open with `TopLevel.Launcher` or `Process.Start` with platform guard).
2. If `!IsDownloadAllowed`, Download stays **disabled** (no override in v1).
3. **Disk space gate:** before the confirmation dialog, call `IDiskSpaceChecker` in
   Core. Required free space = `ApproxDownloadBytes × 1.1` (10 % headroom for partial
   writes and Ollama unpack). Check the volume that holds `%LocalAppData%/ReVitae/`
   (v1 proxy for where local AI assets will be managed). If insufficient or unknown,
   block download and show localized error `AiSetupInsufficientDiskSpace` with
   `{0}` = required size, `{1}` = available size (or “unknown” when probe fails). Do
   **not** start `POST /api/pull` when the check fails.
4. Show confirmation dialog summarizing **model name + approximate download size**.
5. On confirm, call Ollama **`POST /api/pull`** with `{ "name": "<tag>" }` streaming
   response; show **determinate progress** when JSON lines include `completed` /
   `total`, otherwise indeterminate; status text from `status` field when present.
6. On success, show “Model ready”; persist via `AiSettingsStorage` (Core helper, same
   base path pattern as `ProfilePhotoStorage.GetDefaultStorageDirectory()` parent):
   `%LocalAppData%/ReVitae/ai-settings.json`:

   ```json
   {
     "selectedModelId": "medium-instruct",
     "ollamaModelTag": "llama3.1:8b-instruct",
     "downloadedAtUtc": "2026-05-21T12:00:00Z"
   }
   ```

7. On failure, show error + **Retry** (re-issues pull).

**Never** start a download without explicit user confirmation.

If pull is in progress: **allow close** — cancel the HTTP request via
`CancellationToken`; do not block the window.

## UI Structure

### AXAML

Add `AiSetupModalOverlay` + `AiSetupModalPanel` to `MainWindow.axaml` (same grid layer
as other modals).

Suggested regions:

| Region | Content |
| ------ | ------- |
| Header | Title + close |
| Loader panel | `AiDetectionProgressPanel` — visible during detection |
| Content panel | `AiDetectionSummaryTextBlock`, **privacy banner**, recommended card, `ItemsControl` / list for catalog |
| Footer | Download primary (enabled when model selected + Ollama reachable + `IsDownloadAllowed`), Close |

Use existing classes: `re-vitae-app-title`, `re-vitae-secondary`, `re-vitae-primary`,
`re-vitae-app-card` for model cards. Recommended card gets a subtle accent border or
badge “Recommended”.

### Code-behind / partial

- `MainWindow.AiSetup.cs` (new partial) — open/close, detection orchestration,
  download orchestration,
- optional `IAiSetupModalPresenter` thin helper if it keeps `MainWindow` readable.

Detection and pull run on **background threads** (`Task.Run` or `async` with
`ConfigureAwait(false)`); UI updates via `Dispatcher.UIThread.Post`. Support
**CancellationToken** when modal closes during detection or pull.

### Localization

Add keys under `TranslationKeys` / `AppLocalizer` (EN + SK minimum). Follow existing
dot-notation (`action.*`, `modal.*`):

| Key constant | Example EN |
| ------------ | ---------- |
| `OpenAiSetup` → `action.openAiSetup` | Open AI setup |
| `AiSetupTitle` → `modal.aiSetup.title` | AI setup |
| `AiSetupDetecting` → `modal.aiSetup.detecting` | Detecting your system… |
| `AiSetupDetectionFailed` → `modal.aiSetup.detectionFailed` | Could not read system information. |
| `AiSetupRetry` → `modal.aiSetup.retry` | Retry |
| `AiSetupSystemSummary` → `modal.aiSetup.systemSummary` | {0} · {1} · {2} RAM |
| `AiSetupRecommended` → `modal.aiSetup.recommended` | Recommended for your device |
| `AiSetupRequiresMoreMemory` → `modal.aiSetup.requiresMoreMemory` | Requires more memory than detected |
| `AiSetupDownload` → `modal.aiSetup.download` | Download model |
| `AiSetupDownloadConfirm` → `modal.aiSetup.downloadConfirm` | Download {0} (~{1})? This may use significant disk space. |
| `AiSetupOllamaNotRunning` → `modal.aiSetup.ollamaNotRunning` | Ollama is not running. Start Ollama and try again. |
| `AiSetupPullProgress` → `modal.aiSetup.pullProgress` | Downloading… {0} |
| `AiSetupPullComplete` → `modal.aiSetup.pullComplete` | Model downloaded and ready. |
| `AiSetupPullFailed` → `modal.aiSetup.pullFailed` | Download failed. |
| `AiSetupAlreadyDownloaded` → `modal.aiSetup.alreadyDownloaded` | Already on this computer |
| `AiSetupUnknownRam` → `modal.aiSetup.unknownRam` | Could not read total memory; recommendation may be conservative. |
| `AiSetupPrivacyNote` → `modal.aiSetup.privacyNote` | System detection runs only on this device. ReVitae does not send your hardware profile or CV data to ReVitae servers. |
| `AiSetupInsufficientDiskSpace` → `modal.aiSetup.insufficientDiskSpace` | Not enough free disk space. About {0} is required; {1} available. |
| `AiSetupDiskSpaceUnknown` → `modal.aiSetup.diskSpaceUnknown` | unknown |

Register all new keys in `TranslationKeys.All` (or equivalent registry used by tests).

Follow existing `{0}` placeholder patterns.

## Architecture Diagram

```text
Header [AI icon]
        │
        ▼
MainWindow → AiSetupModal (UI)
        │
        │  each open (parallel)
        ├─► ISystemProfileDetector (Core)
        └─► IOllamaRuntimeProbe (Core or ReVitae with HttpClient)
        │
        ▼
AiModelRecommendationService.Recommend(profile, ollama)
        │
        ▼
Render catalog + highlight RecommendedModel
        │
        │  user confirms Download
        ▼
IDiskSpaceChecker (Core) — gate before pull
        │
        ▼
OllamaPullClient → POST /api/pull
        │
        ▼
AiSettingsStorage → ai-settings.json
```

**Separation:** Core owns catalog, recommendation math, settings persistence, and RAM/OS
detection; HTTP clients (`IOllamaRuntimeProbe`, `OllamaPullClient`) may live in
`ReVitae.Core/Ai/Ollama/` with injectable `HttpMessageHandler` for tests.

## Tests

Add under `tests/ReVitae.Tests/Ai/`:

| Test | Assert |
| ---- | ------ |
| `Recommend_With8GbRam_SelectsSmallModel` | Small tier recommended; Medium/Large not allowed |
| `Recommend_With16GbRam_SelectsMedium` | Medium recommended; Large not allowed |
| `Recommend_With80GbRam_CanSelectLarge` | Large allowed with headroom |
| `Recommend_WithUnknownRam_IsConservative` | warning + Small only allowed |
| `Recommend_FlagsOversizedModels` | Large entry `IsDownloadAllowed = false` on 16 GB profile |
| `Catalog_EntriesHaveUniqueIds` | static catalog integrity |
| `OllamaPullClient_ParsesStreamLines` | fake `HttpMessageHandler` |
| `AiSettingsStorage_RoundTrips` | JSON read/write |
| `DiskSpaceChecker_HasSpaceForDownload` | passes when free ≥ bytes × 1.1 |
| `DiskSpaceChecker_BlocksWhenInsufficient` | fails below threshold |
| UI smoke (optional) | modal opens, loader visible, fake detector completes |

Use **`FakeSystemProfileDetector`** and **`FakeOllamaRuntimeProbe`** in tests; never
require real RAM or Ollama in CI.

Target: full suite remains green; add ~10–20 focused tests.

## Documentation Updates

| File | Update |
| ---- | ------ |
| [`README.md`](../README.md) | Mention AI setup modal (local models, Ollama prerequisite) under Roadmap / Phase 2 |
| [`docs/concept.md`](../docs/concept.md) | Note header AI icon delivers on-demand detection (first-launch gate optional later) |
| [`CHANGELOG.md`](../CHANGELOG.md) | Unreleased entry for AI setup modal |
| **New** [`docs/ai-setup.md`](../docs/ai-setup.md) | User-facing: open AI icon, detection, recommended model, Ollama requirement, local-only privacy note, disk space prerequisite, no cloud yet |

Update README prompts map line to `001–036`.

## Out of Scope (Follow-Up Prompts)

- **037** — Cloud provider (OpenAI-compatible) configuration in same modal,
- **038** — First AI feature (improve work description / quality hint assist),
- **039** — AI-assisted import fallback,
- Bundled Ollama installer / one-click Ollama setup wizard,
- GPU / VRAM detection for recommendation v2,
- First-launch auto-open of AI modal before intro dismisses,
- Removing old Ollama models when switching selection.

## Acceptance Criteria

1. New **AI icon** appears in the header toolbar (after Upload, before Setup) with
   tooltip and accessible name; disabled during blocking intro/import overlays.
2. Clicking opens an in-window modal; **Escape** and close buttons dismiss it without
   losing CV form state; other content modals close via `HideOtherContentModals`.
3. **Every open** shows a loader, runs detection asynchronously, then shows results.
4. UI displays OS/arch/RAM summary and **one highlighted recommended model**.
5. Full catalog lists all v1 models with size and minimum RAM.
6. Models with insufficient detected RAM have **Download disabled**
   (`IsDownloadAllowed == false`) and visible “requires more memory” copy.
7. Download requires confirmation and uses Ollama pull when Ollama is reachable.
8. **Disk space check** blocks pull when free space on the local-data volume is below
   `ApproxDownloadBytes × 1.1`; user sees a localized error (no partial download).
9. **Privacy banner** visible in Ready state; states detection is local-only and no
   data is sent to ReVitae servers (EN + SK).
10. Detection + recommendation logic lives in **Core** with unit tests; HTTP probes
   mockable.
11. `./scripts/format-cs.sh`, `npm run lint`, and `./scripts/test.sh` pass.

## Suggested Implementation Order

1. Core types + static catalog + `AiModelRecommendationService` + `AiSettingsStorage` +
   `IDiskSpaceChecker`,
2. `ISystemProfileDetector` + platform RAM readers + `IOllamaRuntimeProbe`,
3. AXAML modal shell + loader / state panels + privacy banner,
4. Wire header button + `HideOtherContentModals` + Escape stack,
5. `OllamaPullClient` + disk gate + progress UI,
6. Localization + docs + tests.

## Expected Result

ReVitae has a visible **AI** entry point. Users can open the modal at any time, see
their system analyzed with a clear loader, receive a **hardware-appropriate local model
recommendation**, and optionally download that model through Ollama — with disk-space
validation, a clear local-only privacy note, no silent downloads, and no AI changes to
CV content yet.
