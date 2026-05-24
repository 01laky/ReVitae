# AI setup (local models)

ReVitae includes an **AI setup** modal for choosing and downloading a **local
Ollama model** on your computer. This is the first Phase 2 building block — the
app does **not** yet use AI to rewrite CV content or run import extraction.

## Open the modal

1. Dismiss the intro overlay if it is still open.
2. Click the **robot icon** in the header toolbar (between **Upload CV** and
   **Setup**).
3. The modal runs **system detection every time** it opens (loader, then results).

## What you see

### Your system

A summary card shows locally detected information:

- Operating system and CPU architecture
- CPU core count
- Total RAM (best effort; may show “unknown” on some setups)
- Free disk space on the ReVitae local-data volume
- Ollama status (running or not, and how many models are already installed)

Detection runs **only on this device**. ReVitae does not send your hardware
profile or CV data to ReVitae servers.

### Recommended model

One model is highlighted as the best fit for your RAM tier. You can still pick
any other allowed model from the list.

### All models

The catalog lists **11 curated Ollama instruct models** (Gemma, Phi-3, Llama,
Qwen, Mistral, Mixtral). Each row shows approximate download size and minimum
RAM.

**Download rules:**

| Situation                                  | Behavior                                                     |
| ------------------------------------------ | ------------------------------------------------------------ |
| Fits your RAM                              | Download enabled                                             |
| **One tier larger** than your strict fit   | Download enabled with a **warning** (may run slowly or fail) |
| Two or more tiers above                    | Download disabled                                            |
| Already installed in Ollama                | “Already on this computer”; Download hidden                  |
| Not enough free disk (~110% of model size) | Error before download starts                                 |

## Prerequisites

- **[Ollama](https://ollama.com)** installed and running locally
  (`http://127.0.0.1:11434`)
- Enough free disk space for the chosen model
- Sufficient RAM for the tier you select (respect warnings for oversized picks)

ReVitae does **not** install Ollama for you in the current version.

## Download flow

1. Select a model card.
2. Click **Download model**.
3. Confirm the dialog (extra warning text when you picked an oversized model).
4. ReVitae calls Ollama `POST /api/pull` and shows progress.
5. On success, selection is saved to
   `%LocalAppData%/ReVitae/ai-settings.json` (model id, tag, timestamp).

You can close the modal during detection or download; in-progress HTTP requests
are cancelled.

## Settings file

```json
{
  "selectedModelId": "llama31-8b",
  "ollamaModelTag": "llama3.1:8b-instruct",
  "downloadedAtUtc": "2026-05-21T12:00:00Z"
}
```

No API keys or secrets are stored.

## Related docs

- Product concept (Phase 2): [`concept.md`](concept.md)
- Implementation prompt: [`../prompts/036-ai-setup-modal-system-detection.md`](../prompts/036-ai-setup-modal-system-detection.md)

## Not in scope yet

- Cloud / OpenAI-compatible providers
- AI-assisted import or field rewrite
- Bundled Ollama installer
- Automatic first-launch wizard
- Removing old Ollama models when switching
