# Docflow AI (.NET)

End-to-end pipeline for extracting information from documents with **LLMs** and word-level **bounding boxes**. The project integrates:

- **MarkItDownNet** (.NET submodule) to convert **PDF/images → Markdown** and normalized word-level BBoxes.
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
- **Docker:** single production-ready container (`9.0-noble`) with model downloaded using `HF_TOKEN`.

## Job Queue

1. Integrated **Hangfire** (MemoryStorage), **SQLite** via **Entity Framework Core** and **Rate Limiting** with paged `GET /api/v1/jobs`.
2. Added `POST /api/v1/jobs` for submission (base64/multipart), `GET /api/v1/jobs/{id}` and `DELETE /api/v1/jobs/{id}` with state managed exclusively by the database and artifacts stored on disk.

### Database Providers

The job queue persistence layer uses Entity Framework Core and supports the following providers:

- **InMemory** — convenient for tests and temporary runs.
- **SQLite** — default lightweight storage.

Switch the provider through `JobQueue:Database` in `appsettings.*.json`.

#### Adding a new provider

1. Add the appropriate EF Core package for the target database.
2. Extend the `switch` statement in `Program.cs` to call `Use<Provider>()`.
3. Configure `JobQueue:Database:Provider` and `ConnectionString` with the new settings.

## High-level Architecture

```
Input (PDF/JPG/PNG)
      │
      ▼
 MarkItDownNet (PDF → text; OCR fallback; words + BBox normalized [0..1])
      │            └─ Tesseract/Leptonica x64 included (no system packages)
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

## Requirements

- **.NET SDK 9.0** (or use Docker)
- Initialized **MarkItDownNet** submodule
- Optional Tesseract tessdata for OCR languages

## MarkItDownNet

- Converts **PDF/images** to **Markdown** with positional metadata:
  - **Word-level BBox:** `[x,y,w,h]` normalized `[0..1]`, origin top-left
  - `FromOcr` per word for diagnostics
- OCR fallback: PDF → image via **PDFtoImage** + **Tesseract** when native words are scarce
- **Tesseract/Leptonica** (linux x64) included under `src/MarkItDownNet/TesseractOCR/x64`
- Options: `OcrDataPath`, `OcrLanguages ("ita+eng")`, `PdfRasterDpi`, `MinimumNativeWordThreshold`, `NormalizeMarkdown`

### Build & Test (local SDK)

```bash
# install locally if needed
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
~/.dotnet/dotnet --version

# build & test
~/.dotnet/dotnet build
~/.dotnet/dotnet test
```

## Docker build

```bash
export HF_TOKEN=hf_************************

DOCKER_BUILDKIT=1 docker build \
  --secret id=hf_token,env=HF_TOKEN \
  --build-arg LLM_MODEL_REPO=unsloth/Qwen3-1.7B-GGUF \
  --build-arg LLM_MODEL_FILE=Qwen3-1.7B-UD-Q4_K_XL.gguf \
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
- **MarkItDownNet submodule:** docs under `src/MarkItDownNet/`
- **Real API Test Plan:** [docs/real-api-test-plan.md](docs/real-api-test-plan.md)
- **Test Report:** [docs/test-report.md](docs/test-report.md)

— end —
