# AI-assisted CV import

ReVitae uses a **deterministic-first** import pipeline (PdfPig, OCR, heuristics,
structured mappers). When that path fails or produces a thin draft, you can
optionally run **batched AI extraction** through the same active backend as
**Improve with AI**.

AI import is **never silent**: you always review a section summary diff before
anything is applied to the form.

See also: [`import-formats.md`](import-formats.md) (format routing),
[`ai-setup.md`](ai-setup.md) (backend configuration).

## Overview

```mermaid
flowchart LR
    A[Import file] --> B[Deterministic parser]
    B -->|success enough| C[Form draft]
    B -->|fail or thin| D{AI backend ready?}
    D -->|no| E[Manual edit / retry]
    D -->|yes| F[Batched AI extraction]
    F --> G[Review summary diff]
    G -->|Apply| C
```

1. **Text-route files** (PDF, DOCX, TXT, images with OCR, …) run through
   `CvTextImportCoordinator`, which always retains **normalized plain text** even
   when field extraction fails.
2. When triggers match, the UI offers **Try AI import** (after failure) or
   **Enhance with AI** (after partial success).
3. `AiCvImportService` sends **small sequential JSON slices** to the model,
   merges fragments via `ReVitaeJsonMapper`, and shows a **review modal**.
4. Only after **Apply** does `ApplyCvImportResult` hydrate the form.

Structured imports (`.revitae.json`, JSON Resume, Europass, CSV with mapper success,
**≥ 5 populated sections**) skip the AI path.

**ReVitae-owned PDF exports:** when PdfPig + ReVitae-aware heuristics
recover a rich draft (typically **> 2** populated sections), the **Enhance with AI**
banner usually does not appear. AI remains available for scanned PDFs, third-party
layouts, and thin parses.

## When AI import is offered

All of the following must hold:

- Text-based import route (not a clean structured success),
- Normalized source text **≥ 80** non-whitespace characters,
- Active AI backend configured and reachable,
- At least one trigger below.

| Trigger              | User-facing situation                                        |
| -------------------- | ------------------------------------------------------------ |
| Deterministic failed | Import error “no structured data” but OCR/text was acquired  |
| Thin draft           | Import succeeded but **≤ 2** sections populated              |
| Low confidence       | OCR used or many low-confidence fields with **≤ 4** sections |
| Partial parse (045)  | Import succeeded with **3–4** sections — offers **Enhance**  |
| Has low fields (045) | **≥ 1** low-confidence field — offers **Fix fields with AI** |
| User requested       | You click **Try AI import** or **Enhance with AI**           |

**Not offered** when the file is too short, unreadable with no text, or a
structured format already imported successfully with enough sections.

ReVitae picks the **least-invasive** default action: failed/thin → **Try AI import**
(replace all); partial (3–4 sections) → **Enhance with AI** (fill empty only when the
form is dirty); success with only uncertain fields → **Fix fields with AI** (targeted
repair, below).

## Targeted field repair (v0.2.12)

When a parse succeeds but some fields are low-confidence, **Fix fields with AI** repairs
**only those fields** instead of re-extracting whole sections.

```mermaid
flowchart LR
    low["Low-confidence fields\n(FieldConfidences)"]
    join["Join with parsed doc\n→ repair targets"]
    cap["Cap at 25, lowest\nconfidence first"]
    batch["Per-section batches\n(source window)"]
    parse["Parse N: value"]
    review["Review before→after"]
    apply["Apply only targeted fields"]

    low --> join --> cap --> batch --> parse --> review --> apply
```

- **No new entries** — repair only rewrites the value of fields the deterministic parser
  already produced; it never adds or removes entries, and never touches photos or ids.
- A field the model leaves unchanged or empty **keeps its current value**.
- **Cap (C.9):** at most **25** fields per run, lowest confidence first; when more exist,
  the review modal shows **“N more uncertain fields not included.”** — never a silent cut.
- **Review before apply** — a per-field **before → after** table; nothing is written until
  you confirm.

## Model batching

Input is **plain text only** (no PDF/image bytes to the model in v1). Batch sizes
depend on the resolved **model profile**:

| Tier       | Example models           | Max input / call | Work entries / batch |
| ---------- | ------------------------ | ---------------- | -------------------- |
| Compact    | Gemma 2 2B               | 1 200 chars      | 1                    |
| Small      | Phi-3 mini, Llama 3.2 3B | 2 400 chars      | 2                    |
| Medium     | Mistral 7B, Llama 3.1 8B | 5 000 chars      | 4                    |
| Large      | Mixtral 8×7B, GPT-4o     | 10 000 chars     | 8                    |
| ExtraLarge | Llama 3.1 70B            | 16 000 chars     | 12                   |

Compact profiles combine **skills + languages** in one phase and use overlapping
windows when section headers are missing.

```mermaid
flowchart TB
    subgraph phases [Sequential phases Compact tier example]
        P1[personal] --> P2[work batch 1..N]
        P2 --> P3[education batch 1..N]
        P3 --> P4[skills + languages]
        P4 --> P5[certificates / projects / links / additional]
    end
```

Progress shows **Step X of Y** from the dynamic batch plan (not a fixed step count).

## Review before apply

The review modal shows:

- Active backend (local model or online provider),
- **Section summary** table (before vs after AI — counts / partial / complete),
- Warning: AI-generated — review before export,
- Note that **profile photos are not extracted**,
- **Apply to form**, **Fill empty fields only** (when merging into an edited draft),
  **Cancel**.

Partial batch failures add `ImportWarningAiPartial`.

## Merge modes

```mermaid
flowchart TB
    subgraph merge [Merge modes]
        R[Replace all]
        F[Fill empty only]
    end
    R --> M[CvImportResult via ReVitaeJsonMapper]
    F --> M
    M --> A[ApplyCvImportResult]
```

- **Try AI import** after deterministic **failure** → default **Replace all**.
- **Enhance with AI** on a clean partial import → default **Replace all**.
- **Enhance** when the form is **dirty** → prefer **Fill empty only** to avoid
  clobbering edits (041 unsaved guard).

Existing profile photo paths are **preserved** on Enhance; AI never writes photo fields.

## Language behavior

- Extracted field values keep **source spelling** (names, employers, places).
- UI locale (`en` / `sk`) affects instruction strings only — CV content is not
  translated by the import prompts.
- Slovak names with diacritics remain unchanged when the UI is in Slovak.

## Profile photos

AI import fills **text fields only**. Photos from PDF/scans are **not** extracted.
Upload a photo manually after import. On Enhance, an existing uploaded photo is kept.

## Privacy

- **Local Ollama:** CV text stays on your machine; no extra session confirm.
- **Online providers:** first multi-step send shows the same session confirm as
  **Improve with AI**; CV excerpts are sent sequentially in small batches.

## Debug logging

Set environment variable:

```bash
export REVITAE_IMPORT_DEBUG=1
# optional custom log path:
export REVITAE_IMPORT_DEBUG_LOG=/tmp/revitae-import-debug.log
```

Logged: phase, batch index, char counts, parse success, duration — **not** full CV
text or email addresses. AI batch lines use the `ai-import` step prefix alongside
existing `CvImportDiagnosticsLogger` import traces. The **section advisor** and
**field repair** (045) add `ai-advisor` and `ai-repair` step prefixes, logging task
kind, section, char/field counts, entity-guard hits, cache hit/miss, and duration —
again **never** CV text, emails, or model output bodies.

When debug is enabled, the review modals show an optional **Details** expander with
sanitized parse and guard notes.

## Related prompts

- **032** — OCR text feeds AI import when heuristics fail
- **039** — shared backend, online confirm, `uiCulture` in prompts
- **041** — dirty-project guard on Enhance replace
- **045** — section advisor, broadened hints, target-role context, entity guard,
  targeted field repair, broadened import triggers
