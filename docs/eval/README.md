# Evaluation of BBox anchoring strategies on XFUND IT (subset 10)

This document summarizes the results produced by the evaluation runner on the first ten documents of the Italian **XFUND** *val* split. The goal is to compare four BBox identification systems using values extracted by the LLM.

> Output artifacts: `eval/out/summary.csv`, `eval/out/coverage_matrix.csv`, `eval/out/<file>/<strategy>.json` and, if enabled, per-algorithm traces in `eval/out/traces/…`. Code and project structure live in the main repository. ([GitHub][1])

---

## Executive summary

- **Anchoring precision**: **Pointer / WordIds** offers the best balance between BBox coverage and lexical fidelity thanks to verifiable token pointers.
- **Noise robustness**: **TokenFirst** handles noisy OCR or split fields well but can yield false positives near labels.
- **Baseline**: **Legacy** variants (BitParallel and Classic Levenshtein) are useful for compatibility but generally show lower BBox coverage than pointer-based strategies.
- **Offset trade-off**: **Pointer / Offsets** is close to WordIds when the Text View is clean but may degrade with aggressive normalizations (accents, spacing) or overly wide spans from the LLM.

**Practical recommendation:** set **Pointer / WordIds** as default with **TokenFirst** as automatic fallback; keep **Legacy** for regression testing and historical compatibility.

---

## Dataset & protocol

- **Source:** XFUND IT (*val*). The runner downloads and unzips `it.val.zip`, selects the first ten documents alphabetically, and builds a manifest per file using XFUND annotations: `question/key/header` nodes linked to `answer/value` determine expected labels and values.
- **Coordinate normalization:** `expectedBoxes` are converted to `[x,y,w,h]` normalized in `[0..1]` for resolution-independent comparison.
- **Words & BBox:** each image is processed with **MarkItDownNet** (in-process, .NET) to obtain words and normalized BBoxes, producing:
  - **Index Map** (Pointer/WordIds) with annotations `[[W{page}_{idx}]]`
  - **Text View** (Pointer/Offsets) mapping offsets to word indices

---

## Compared strategies

1. **Pointer / WordIds** – the LLM returns `wordIds[]` from IDs in the Index Map; the resolver maps them 1:1 to tokens and merges the BBoxes.
2. **Pointer / Offsets** – the LLM returns `offsets {start,end}` on the Text View; offsets are mapped to nearby tokens and thus to BBoxes.
3. **TokenFirst** – candidate search and refinement with distance (bit-parallel or classic, configurable); favors contiguity, proximity to the label, and normalized similarity.
4. **Legacy** – historical implementation evaluated separately with:
   - `Legacy-BitParallel`
   - `Legacy-ClassicLevenshtein`

Further sections in the original Italian version describe metrics, detailed results, error analysis, and performance; refer to the generated CSV/JSON artifacts for full data.

[1]: https://github.com/mapo80/docflow-ai-net "GitHub - mapo80/docflow-ai-net"
