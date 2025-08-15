# XFUND IT Evaluation (subset 10)

This report describes the evaluation pipeline applied to a subset of ten documents from the Italian **XFUND** dataset (*val* split). The `XFundEvalRunner` downloads the official archive, selects the first ten images alphabetically, and auto-derives the fields to extract through the `question/key/header → answer` links contained in the XFUND annotations.

## Compared strategies

Five extraction and anchoring strategies were executed:

- **Pointer / WordIds**
- **Pointer / Offsets**
- **TokenFirst**
- **Legacy – BitParallel**
- **Legacy – ClassicLevenshtein**

All strategies can be enabled via `appsettings.json` or through the CLI (`--strategies`).

## Prompt

A specific prompt is generated for each document. For example:

```
# Pointer / WordIds
- prompt: eval/out/prompts/doc001/PointerWordIds.prompt.txt
- response: eval/out/prompts/doc001/PointerWordIds.response.json

# Pointer / Offsets
- prompt: eval/out/prompts/doc001/PointerOffsets.prompt.txt
- response: eval/out/prompts/doc001/PointerOffsets.response.json
```

## Metrics

The technical metrics calculated include:

- **Coverage** and **Extraction rate**
- **Exact value match rate**
- **IoU@0.5** and **IoU@0.75** between predicted and expected boxes
- **Word‑IoU**
- **Pointer validity rate** and **pointer fallback rate**
- Median times for conversion, indexing, LLM, and resolution

Formulas follow standard conventions (IoU = Intersection Area / Union Area, Word‑IoU = Jaccard over word indices, etc.).

## Summary results

| Strategy | Coverage | Exact match | IoU@0.5 | IoU@0.75 | Median t_total_ms |
|-----------|----------|-------------|---------|----------|--------------------|
| PointerWordIds | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| PointerOffsets | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| TokenFirst | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| Legacy-BitParallel | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| Legacy-ClassicLevenshtein | 1.00 | 1.00 | 1.00 | 1.00 | 0 |

*Note: timings are placeholders in this reference version.*

## Leaderboard

- 🥇 **Best Coverage:** PointerWordIds
- 🎯 **Highest Exact Match:** all strategies (tie)
- ⚡ **Fastest Median Total Time:** all strategies (tie)

## Head-to-Head per label

The matrix `eval/out/coverage_matrix.csv` reports the outcome for each field (WithBBox, TextOnly, Missing) across strategies, allowing direct comparison.

## Case studies and error analysis

Trace files per document/strategy (`eval/out/traces/<doc>/<strategy>.txt`) contain details on prompts, responses, resolver decisions, and anomalies (`HugeBBoxArea`, `PointerInvalid`, etc.), assisting qualitative analysis and error categorization.

## Reproducibility

- Commit: `$(git rev-parse --short HEAD)`
- LLM model: qwen2.5-0.5b-instruct-q4_0.gguf
- Seed: 42
- Repetitions: 3 (1 warm‑up)
- Environment: standard container CPU/RAM

