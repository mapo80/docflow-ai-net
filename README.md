# docflow-ai-net

.NET 9 API + Python FastAPI sidecar for OCR→Markdown (MarkItDown+Tesseract) and LLM extraction (LLamaSharp + GGUF).

## Highlights
- Clean layering (Domain / Application / Infrastructure / API)
- Controllers sottili, logica nei services
- **Swagger + API Key** (`X-API-Key`)
- **Serilog** logs (console + rolling files)
- **Polly** retry/timeout sul client MarkItDown
- **GBNF grammar** sempre attiva → output **JSON valido**
- **Thinking/no_think** per-request (`X-Reasoning`) o da config `LLM:ThinkingMode`
- **Extraction Profiles** (schema campi) + validazione post-LLM

## Avvio rapido con Docker Compose
```bash
cd deployment
docker compose up --build
# API: http://localhost:5214  |  MarkItDown: http://localhost:8000
```
Modello GGUF: metti il file in `./models` (es. `SmolLM-135M-Instruct.Q4_K_S.gguf`).

### Download del modello GGUF
Scarica il modello leggero per i test in `./models` usando un token Hugging Face:
```bash
export HF_TOKEN="<il tuo token>"
huggingface-cli download MaziyarPanahi/SmolLM-135M-Instruct-GGUF \
  SmolLM-135M-Instruct.Q4_K_S.gguf \
  --local-dir ./models --token "$HF_TOKEN"
export LLM__ModelPath="$(pwd)/models/SmolLM-135M-Instruct.Q4_K_S.gguf"
```

### Dataset e test
Nel repo è presente un dataset di esempio sotto `./dataset`.
Per eseguire i test (inclusi quelli sul dataset):
```bash
export MSBUILDTERMINALLOGGER=false
dotnet test
python -m pytest
```

## All-in-one Docker (API + Python nello stesso container)
```bash
cd deployment
docker build -f Dockerfile.allinone -t docflow-ai-net:allinone ..
docker run --rm -p 5214:8080 -p 8000:8000 -v $(pwd)/../models:/models:ro docflow-ai-net:allinone
```

## Endpoint principali
- `POST /api/v1/process` (protetto API key) — multipart `file=@image`
  - `X-Reasoning: think|no_think|auto`
- `GET /health`
- Swagger: `/swagger`

## Config (estratto)
Vedi `src/DocflowAi.Net.Api/appsettings.json`. Override via env (`LLM__ModelPath`, ecc.).

## Smoke Test
```bash
cd smoke
./smoke.sh
```
Salva `markitdown.json` e, se presente il modello, `process.json`.

## agents.md
Consulta `agents.md` per l’uso con strumenti di codegen.
