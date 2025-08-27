# Docflow AI (.NET)

End-to-end pipeline for extracting information from documents with **LLMs** and word-level **bounding boxes**. The project integrates:

- BBox anchoring strategies:
  - **Pointer / WordIds** (primary)
  - **Pointer / Offsets**
  - **TokenFirst** (Aho-Corasick with fuzzy token distance; Bit-parallel or classic Levenshtein)
  - **Legacy** (BitParallel and Classic variants evaluated separately)
- **LLamaSharp** to run **GGUF** models in-process (no Python), packaged in a single **.NET 9.0-noble** Docker image.
- CLI evaluation tools and Markdown reports.

## TL;DR

- **Use case:** field extraction from forms/invoices/free layout documents with spatial evidence (BBox).
- **Recommended strategy:** **Pointer / WordIds** → fallback **TokenFirst** → optional **Legacy** for compatibility.
- **Evaluation:** see `docs/eval/` and [XFUND IT Evaluation](docs/XFUND_IT_Eval.md).
- **Dataset CLI:** `dotnet run --project tools/DatasetCli -- --dataset <path> --output <file>`
- **Docker:** single production-ready container (`9.0-noble`) with model downloaded using `HF_TOKEN`.

## Job Queue

1. Integrated **Hangfire** (MemoryStorage), **SQLite** via **Entity Framework Core** and **Rate Limiting** with paged `GET /api/v1/jobs`.
2. Added `POST /api/v1/jobs` for submission (base64/multipart), `GET /api/v1/jobs/{id}` and `DELETE /api/v1/jobs/{id}` with state managed exclusively by the database and artifacts stored on disk.

### Default Records

At startup the API can optionally seed two sample jobs and a sample template using files from the `dataset` directory. The flag `JobQueue:SeedDefaults` in `appsettings.json` controls this behavior and defaults to `true`.

The template token is `template` and contains the invoice extraction prompt and field schema. It is referenced by the seeded jobs and can be reused when submitting new jobs by specifying `"template"` as the template token.

### Job submission parameters

Each job must reference two tokens:

- `model` — the model token to execute (see `/api/models` for available models).
- `templateToken` — the template token describing the prompt and fields (see `/api/templates`).

Clients should send these tokens to `POST /api/v1/jobs` and they are persisted on the job record. The job detail endpoint returns the tokens so that consumers can look up full model or template information later.

### Database Providers

The job queue persistence layer uses Entity Framework Core and supports the following providers:

- **InMemory** — convenient for tests and temporary runs.
- **SQLite** — default lightweight storage.

Switch the provider through `JobQueue:Database` in `appsettings.*.json`.

#### Adding a new provider

1. Add the appropriate EF Core package for the target database.
2. Extend the `switch` statement in `Program.cs` to call `Use<Provider>()`.
3. Configure `JobQueue:Database:Provider` and `ConnectionString` with the new settings.

## Model dispatch service

Jobs now specify a **model token** that determines which backend executes the
LLM request. The `ModelDispatchService` resolves the token to a `ModelDocument`
and routes the call to the appropriate system:

- **Local models** &mdash; return the payload directly to the in-process
  pipeline.
- **Hosted LLM (OpenAI)** &mdash; uses the official `OpenAI` SDK to invoke chat
  completions with bearer authentication.
- **Hosted LLM (Azure OpenAI)** &mdash; uses the `Azure.AI.OpenAI` SDK to call the
  specified deployment with the `api-key` header.

All hosted calls run behind a Polly retry policy with exponential backoff and
jitter, attempting up to three times before surfacing an error.

The service throws when the model token is unknown or when the provider type is
not supported. It is consumed by the job `ProcessService` to execute jobs with
the requested model.

## High-level Architecture

```
Input (PDF/JPG/PNG)
      │
      ▼
 Markdown conversion via Docling Serve (PDF → text; OCR fallback; words + BBox normalized [0..1])
      │
      ▼
 Normalization & Indexing (token, bigram, Index Map / Text View)
      │
      ├─ Prompt LLM (Pointer/WordIds | Offsets | Value-only)
      │
      ▼
 Resolver (Pointer → direct mapping; TokenFirst/Legacy → retrieval + fuzzy + layout heuristics)
      │
      ▼
Output JSON (value, evidence[], wordIndices[], bbox[x,y,w,h], confidence, optional metrics)
```

Docling Serve provides the markdown conversion service. Configure its base URL with `Markdown:DoclingServeUrl` in `appsettings.json`.

## Requirements

- **.NET SDK 9.0** (or use Docker)

## Testing guidelines

- Frontend end-to-end tests are archived under `frontend/donotrun` and must not be executed or created.
- Unit tests for new features and modifications must achieve at least **90%** coverage.

## Docker build

```bash
export HF_TOKEN=hf_************************

DOCKER_BUILDKIT=1 docker build \
  --secret id=hf_token,env=HF_TOKEN \
  --build-arg LLM_DEFAULT_MODEL_REPO=unsloth/Qwen3-1.7B-GGUF \
  --build-arg LLM_DEFAULT_MODEL_FILE=Qwen3-1.7B-UD-Q4_K_XL.gguf \
  --build-arg LLM_MODEL_REV=main \
  -t docflow-ai-net:with-model .

# Run
docker run --rm -p 8080:8080 docflow-ai-net:with-model
```

## BBox anchoring strategies

- **Pointer / WordIds (default):** the LLM returns `["W{page}_{index}", ...]` → deterministic mapping to tokens and merged BBoxes.
- **Pointer / Offsets:** the LLM returns `{start,end}` on a canonical Text View → mapped to nearby tokens.
- **TokenFirst:** token indexing + **Aho-Corasick** for exact matches; fuzzy token-level with **Myers/Bit-parallel** or **classic Levenshtein**; layout-aware disambiguation.
- **Legacy:** historical pipeline based on character distance; useful for baseline and compatibility.

### Configuration (appsettings)

```json
{
  "Resolver": {
    "Strategy": "Auto",  // Auto | Pointer | TokenFirst | Legacy
    ...
  }
}
```

## Evaluation & report

- CLI tool: `tools/XFundEvalRunner` (or `BBoxEvalRunner` depending on branch).
- Features:
  - Downloads **XFUND IT (val)** and selects the first 10 documents.
  - Auto-derives expected labels and values via annotation links.
  - Builds **Index Map** and **Text View**.
  - Runs 5 strategies: `PointerWordIds`, `PointerOffsets`, `TokenFirst`, `Legacy-BitParallel`, `Legacy-ClassicLevenshtein`.
  - Computes quantitative, technical, timing, and qualitative metrics.
  - Saves: `summary.csv`, `coverage_matrix.csv`, per-strategy JSON, and traces (prompt, response, rejection reasons).

## Logging & Telemetry

- Structured **Serilog**: timings per phase, candidates, thresholds, similarity, fallback reasons.
- **EventCounters/OTel** (if enabled): histograms, coverage ratios, counters.
- Rule: truncate logs beyond 2KB to avoid document leakage.

## Troubleshooting

- **Invalid pointers** → check grammar/schema and Index Map; enable `MaxGapBetweenIds=1` for hyphen splits.
- **Offsets out of range** → verify Text View normalization and newlines.
- **Noisy OCR** → try **TokenFirst** with adaptive thresholds (0.25→0.35) and `EnableLabelProximity=true`.
- **Model missing** → provide `HF_TOKEN` and ensure permission on HuggingFace repo; mount volume at `/home/appuser/models` for cache.

## Roadmap

- Learning-to-rank reranker (XGBoost/LightGBM)
- FM-Index/Suffix Automaton for fast exact phrases
- Deterministic plugins for structured fields (IBAN, VAT, CF, dates, amounts)
- Automatic charts (PNG) in reports (coverage, timing, IoU)

## License

MIT (see LICENSE). Some dependencies/assets may have different licenses (e.g., XFUND dataset, HuggingFace models); verify respective terms.

### Quick references

- **Eval:** `docs/eval/` and [docs/XFUND_IT_Eval.md](docs/XFUND_IT_Eval.md)
- **Docker (9.0-noble + LLamaSharp):** see Dockerfile and `start.sh` in root
- **Config:** `appsettings.*.json` (`Resolver`, `LLM`)
- **Real API Test Plan:** [docs/real-api-test-plan.md](docs/real-api-test-plan.md)

— end —
