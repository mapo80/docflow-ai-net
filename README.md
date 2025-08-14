# Docflow AI (.NET)

Pipeline **end-to-end** per l’estrazione di informazioni da documenti con **LLM** e **bounding box** a livello parola.
Il progetto integra:

* **MarkItDownNet** (submodule .NET) per conversione **PDF/immagini → Markdown** e token/word-level **BBox** normalizzate;
* Strategie di **ancoraggio** BBox:

  * **Pointer / WordIds** (primaria)
  * **Pointer / Offsets**
  * **TokenFirst** (Aho-Corasick + fuzzy token-level; distanza edit **Bit-parallel** o **Levenshtein classica**)
  * **Legacy** (storica; BitParallel e Classic valutate separatamente);
* **LLamaSharp** in-process per usare modelli **GGUF** (niente Python), con **Docker** unico basato su **.NET 9.0-noble**;
* Strumenti di **valutazione** (runner CLI) e report Markdown dettagliati.

---

## TL;DR

* **Use case:** estrazione campi da moduli/fatture/forme libere con evidenza spaziale (BBox).
* **Strategia consigliata:** **Pointer / WordIds** → fallback **TokenFirst** → (facoltativo) **Legacy** per compat.
* **Eval pronti:** vedi `docs/eval/` e **[Valutazione XFUND IT (subset 10)](docs/XFUND_IT_Eval.md)**.
* **Docker production-ready:** unico container **9.0-noble**, modello GGUF scaricato con **HF\_TOKEN** esterno.

---

## Job Queue

- Passo 1: integrati **Hangfire** (MemoryStorage), **LiteDB** e **Rate Limiting** con endpoint `GET /v1/jobs` paginato.
- Passo 2: aggiunto `POST /v1/jobs` per il submit (file base64/multipart), `GET /v1/jobs/{id}` e `DELETE /v1/jobs/{id}` con stato gestito solo da LiteDB e artefatti salvati su filesystem.

---

## Architettura (high-level)

```
Input (PDF/JPG/PNG)
      │
      ▼
 MarkItDownNet (PDF → testo; OCR fallback; parole + BBox normalizzate [0..1])
      │            └─ Tesseract/Leptonica native x64 incluse (no pacchetti di sistema)
      ▼
 Normalization & Indexing (token, bigram, Index Map / Text View)
      │
      ├─ Prompt LLM (Pointer/WordIds | Offsets | Value-only)
      │
      ▼
 Resolver (Pointer → mappa diretta; TokenFirst/Legacy → retrieval+fuzzy+layout heuristics)
      │
      ▼
 Output JSON (value, evidence[], wordIndices[], bbox[x,y,w,h], confidence, metriche opz.)
```

---

## Requisiti

* **.NET SDK 9.0** (o usa Docker).
* Submodule **MarkItDownNet** inizializzato.
* (Opzionale) tessdata Tesseract per OCR lingue (se servono).

---

## MarkItDownNet in breve

* Converte **PDF/immagini** in **Markdown** e metadati posizionali:

  * **Word-level BBox**: `[x,y,w,h]` normalizzate `[0..1]`, origine in alto a sinistra.
  * **FromOcr** per parola (utile per penalità/diagnostica).
* Fallback OCR: rasterizza PDF con **PDFtoImage** + **Tesseract** quando le parole native sono poche.
* **Tesseract/Leptonica** (linux x64) sono **incluse** sotto `src/MarkItDownNet/TesseractOCR/x64`, copiate vicino ai binari — non servono pacchetti di sistema.
* Opzioni (esempi): `OcrDataPath`, `OcrLanguages ("ita+eng")`, `PdfRasterDpi`, `MinimumNativeWordThreshold`, `NormalizeMarkdown`.

### Build & Test (SDK locale)

```bash
# install locale (se necessario)
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
~/.dotnet/dotnet --version

# build & test (usare sempre la dotnet locale)
~/.dotnet/dotnet build
~/.dotnet/dotnet test
```

---

## Build container docker

export HF_TOKEN=hf_************************

DOCKER_BUILDKIT=1 docker build \
  --secret id=hf_token,env=HF_TOKEN \
  --build-arg LLM_MODEL_REPO=unsloth/Qwen3-1.7B-GGUF \
  --build-arg LLM_MODEL_FILE=Qwen3-1.7B-UD-Q4_K_XL.gguf \
  --build-arg LLM_MODEL_REV=main \
  -t docflow-ai-net:with-model .

# Esecuzione

docker run --rm -p 8080:8080 docflow-ai-net:with-model

---

## Strategie di ancoraggio BBox

* **Pointer / WordIds (default):** l’LLM restituisce `["W{page}_{index}", ...]` → mappatura **deterministica** ai token e **BBox** di unione.
* **Pointer / Offsets:** l’LLM restituisce `{start,end}` su una “Text View” canonica → mappa a token più vicini.
* **TokenFirst:** indicizzazione token + **Aho-Corasick** per match esatti; fuzzy token-level con **Myers/Bit-parallel** o **Levenshtein classica** (configurabile); disambiguazione layout-aware.
* **Legacy:** pipeline storica su distanza carattere; utile per baseline e compat.

### Configurazione (appsettings)

```json
{
  "Resolver": {
    "Strategy": "Auto",                // Auto | Pointer | TokenFirst | Legacy
    "Order": ["Pointer","TokenFirst","Legacy"],

    "Pointer": {
      "Mode": "WordIds",               // WordIds | Offsets
      "Strict": true,
      "MaxGapBetweenIds": 1,
      "IncludeIndexMapInPrompt": true,
      "MaxPointerTokensInPrompt": 20000,
      "WordIdFormat": "W{Page}_{Index}",
      "ConfidenceWhenStrict": 1.0
    },

    "TokenFirst": {
      "DistanceAlgorithm": "BitParallel",    // BitParallel | ClassicLevenshtein
      "EditDistanceThreshold": 0.25,
      "AdaptiveShortMax": 0.40,
      "AdaptiveLongMax": 0.35,
      "MaxCandidates": 10,
      "EnableLabelProximity": true
    }
  }
}
```

Override via env: `Resolver__Strategy`, `Resolver__Pointer__Mode`, `Resolver__TokenFirst__DistanceAlgorithm`, ecc.

---

## LLM in-process (LLamaSharp) + Docker unico (9.0-noble)

Il progetto usa **LLamaSharp** per caricare modelli **GGUF** in-process (no Python).
In Docker, il modello viene scaricato a runtime da **Hugging Face** con **token** passato dall’esterno.

* Modello di riferimento: **Qwen3-1.7B-UD-Q4\_K\_XL.gguf**
  HuggingFace: `unsloth/Qwen3-1.7B-GGUF`

### Variabili d’ambiente rilevanti

* `HF_TOKEN` **(obbligatoria al primo run)**: token HF con accesso al repo del modello.
* `LLM_MODEL_REPO` (default `unsloth/Qwen3-1.7B-GGUF`)
* `LLM_MODEL_FILE` (default `Qwen3-1.7B-UD-Q4_K_XL.gguf`)
* `LLM_MODEL_REV` (default `main`)
* `LLAMASHARP__ContextSize` (default `8192`)
* `LLAMASHARP__Threads` (default `0` → auto `nproc`)
* `LLM__Provider=LLamaSharp`, `LLM__ModelPath=/home/appuser/models/...` (già impostati nello start script)

### Build & run con Docker (singolo container)

```bash
# build (Dockerfile alla radice)
docker build -t docflow-ai-net:latest .

# run (passa il token HF; monta opzionale volume modelli)
docker run --rm -p 8080:8080 \
  -e HF_TOKEN=hf_xxxxxxxxxxxxxxxxxxxxxxxxx \
  -e LLAMASHARP__ContextSize=8192 \
  -e LLAMASHARP__Threads=0 \
  -v $PWD/models:/home/appuser/models \
  docflow-ai-net:latest
```

L’entrypoint scarica il **GGUF** con `curl` (Authorization: Bearer **HF\_TOKEN**) in `/home/appuser/models` e avvia l’API .NET.

---

## API (schema tipico)

> Gli endpoint possono variare in base alla versione; qui un esempio comune.

L'endpoint legacy `/api/process` è stato rimosso. Usa la coda lavori:

```bash
curl -X POST 'http://localhost:5000/v1/jobs?mode=immediate' \
  -H 'X-API-Key: dev-secret-key-change-me' \
  -F 'file=@dataset/sample_invoice.pdf;type=application/pdf'
```
**Response:**

```json
{
  "DocumentType": "Invoice",
  "Fields": [
    {
      "Key": "company_name",
      "Value": "ACME S.p.A.",
      "Confidence": 0.96,
      "Evidence": [
        {
          "Page": 0,
          "WordIndices": [0, 1],
          "BBox": { "X": 0.40843904, "Y": 0.0551046, "W": 0.18312192, "H": 0.016163632 },
          "Text": "ACME S.p.A.",
          "Score": 1,
          "Label": null
        }
      ],
      "Pointer": null
    },
    {
      "Key": "invoice_date",
      "Value": "2025-08-09",
      "Confidence": 0.9,
      "Evidence": [],
      "Pointer": null
    },
    {
      "Key": "invoice_number",
      "Value": "INV-2025-001",
      "Confidence": 0.9,
      "Evidence": [],
      "Pointer": null
    }
  ],
  "Language": "auto",
  "Notes": "Grazie per acquisto! il tuo"
}
```

---

## Valutazione (eval) & report

### Runner CLI

* Strumento: `tools/XFundEvalRunner` (o `BBoxEvalRunner`, a seconda del branch).
* Funzioni:

  * Scarica **XFUND IT (val)** e seleziona i **primi 10** documenti.
  * Auto-deriva le **label attese** (question/key/header con **link** a answer/value).
  * Costruisce **Index Map** (Pointer/WordIds) e **Text View** (Pointer/Offsets).
  * Esegue **5 varianti**:
    `PointerWordIds`, `PointerOffsets`, `TokenFirst`, `Legacy-BitParallel`, `Legacy-ClassicLevenshtein`.
  * Calcola metriche **Quantitative**, **Tecniche**, **Tempi**, **Qualitative**.
  * Salva: `summary.csv`, `coverage_matrix.csv`, JSON per-strategia, **tracce** (prompt, risposta, motivazioni di scarto).

### Metriche chiave

* **Coverage (BBox)**: `labels_with_bbox / labels_expected`
* **Extraction rate**: `(labels_with_bbox + labels_text_only) / labels_expected`
* **Exact-match** (valore vs atteso normalizzato)
* **Word-IoU (Jaccard su indici)**, **IoU\@0.5 / IoU\@0.75** (bbox normalizzate)
* **Tempi**: `t_convert_ms`, `t_index_ms`, `t_llm_ms`, `t_resolve_ms`, `t_total_ms` (mediana/p95)
* **Pointer validity/fallback**: tasso e cause (`NoPointers`, `InvalidIds`, `NonContiguous`, `OutOfRange`, `EmptyOffsets`)

### Panorama sintetico (esempio indicativo)

> Valori **indicativi** su subset 10, utili per orientarsi. Per i numeri esatti consulta i CSV/JSON in `docs/eval/` e **[report dettagliato](docs/XFUND_IT_Eval.md)**.

| Strategia            | Coverage (BBox) | Exact-match   | Word-IoU      | Mediana t\_total |
| -------------------- | --------------- | ------------- | ------------- | ---------------- |
| Pointer / WordIds    | **0.85–0.95**   | **0.85–0.95** | **0.80–0.90** | **120–180 ms**   |
| Pointer / Offsets    | 0.80–0.90       | 0.80–0.90     | 0.70–0.85     | 130–200 ms       |
| TokenFirst           | 0.70–0.85       | 0.75–0.90     | 0.65–0.80     | 150–220 ms       |
| Legacy – BitParallel | 0.60–0.75       | 0.65–0.80     | 0.55–0.70     | 170–260 ms       |
| Legacy – ClassicLev. | 0.55–0.70       | 0.60–0.75     | 0.50–0.65     | 180–280 ms       |

* **Interpretazione rapida:** Pointer/WordIds è in genere **migliore** su copertura, exact e stabilità, con latenza contenuta; TokenFirst è un **ottimo fallback** nei casi rumorosi; le Legacy restano baseline/compat.

### Dove leggere i risultati

* **Panoramica e guide:** `docs/eval/`
* **Report dettagliato:** **[docs/XFUND\_IT\_Eval.md](docs/XFUND_IT_Eval.md)**
* **Riepilogo per documento/strategia:** `eval/out/summary.csv`
* **Head-to-head per label:** `eval/out/coverage_matrix.csv`
* **Dettagli ed evidenze:** `eval/out/<file>/<strategy>.json`
* **Tracce (prompt/risposta/decisioni):** `eval/out/traces/...`

---

## Logging & Telemetria

* **Serilog** strutturato: tempi per fase, candidati, soglie, similarity, cause di fallback.
* **EventCounters/OTel** (se abilitati): histogram tempi, ratio coverage per strategia, counter di esiti.
* Regola d’oro: troncare log oltre 2KB per evitare leakage di testo documento.

---

## Troubleshooting

* **Pointer invalidi** → verifica grammar/schema e che l’Index Map non sia troncata; abilita `MaxGapBetweenIds=1` per spezzature con trattino.
* **Offset fuori range** → controlla la “Text View” (normalizzazione e newline).
* **OCR rumoroso** → prova **TokenFirst** con soglie adattive (0.25→0.35) e `EnableLabelProximity=true`.
* **Modello non scaricato** → passa `HF_TOKEN` e verifica permessi sul repo HuggingFace; monta un volume su `/home/appuser/models` per cache persistente.

---

## Roadmap

* Reranker **learning-to-rank** (XGBoost/LightGBM) per tie-break.
* FM-Index/Suffix Automaton per exact-phrase ultra-rapida.
* Plugin deterministici per campi strutturati (IBAN, P.IVA, CF, date, importi).
* Grafici automatici (PNG) nei report (coverage, tempi, IoU).

---

## Licenza

MIT (vedi file LICENSE).
Alcune dipendenze/asset possono avere licenze diverse (es. dataset XFUND, modelli HuggingFace): verificare le relative condizioni.

---

### Riferimenti rapidi

* **Eval:** `docs/eval/` e **[docs/XFUND\_IT\_Eval.md](docs/XFUND_IT_Eval.md)**
* **Docker (9.0-noble + LLamaSharp):** vedi Dockerfile e `start.sh` in radice
* **Config:** `appsettings.*.json` (sezione `Resolver`, `LLM`)
* **Submodule MarkItDownNet:** documentazione in `src/MarkItDownNet/`

* **Real API Test Plan:** [docs/real-api-test-plan.md](docs/real-api-test-plan.md)
* **Test Report:** [docs/test-report.md](docs/test-report.md)
— fine —
