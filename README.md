# docflow-ai-net

.NET 9 API with in-process OCR→Markdown via the C# **MarkItDownNet** library and LLM extraction (LLamaSharp + GGUF).

## Highlights
- Clean layering (Domain / Application / Infrastructure / API)
- Controllers sottili, logica nei services
- **Swagger + API Key** (`X-API-Key`)
- **Serilog** logs (console + rolling files)
- Conversione OCR→Markdown totalmente in .NET (nessun servizio Python)
- **Serilog** logs (console JSON)
- **GBNF grammar** sempre attiva → output **JSON valido**
- **Thinking/no_think** per-request (`X-Reasoning`) o da config `LLM:ThinkingMode`
- Schema dei campi definito a runtime (lista di key/format) e validazione post-LLM
 
## Architecture
```
File/PDF/Image -> MarkdownNetConverter -> LlamaExtractor -> JSON Output
```

## Avvio rapido con Docker Compose
```bash
cd deployment
docker compose up --build
# API: http://localhost:5214
```
Modello GGUF: metti il file in `./models` (es. `qwen2.5-0.5b-instruct-q4_0.gguf`).

### Download del modello GGUF
Scarica il modello leggero per i test in `./models` usando un token Hugging Face:
```bash
export HF_TOKEN="<il tuo token>"
huggingface-cli download Qwen/Qwen2.5-0.5B-Instruct-GGUF \
  qwen2.5-0.5b-instruct-q4_0.gguf \
  --local-dir ./models --token "$HF_TOKEN"
export LLM__ModelPath="$(pwd)/models/qwen2.5-0.5b-instruct-q4_0.gguf"
```

### Dataset e test
Nel repo è presente un dataset di esempio sotto `./dataset`.
Per eseguire i test (inclusi quelli sul dataset):
```bash
export MSBUILDTERMINALLOGGER=false
dotnet test
python -m pytest
```
I test esercitano direttamente la libreria MarkItDownNet usando i PDF/PNG in `./dataset`.
Il prompt dell'LLM e la lista dei campi sono passati alla chiamata di processo; i risultati per ogni file vengono stampati nei log dei test.

Per il debug del PDF di esempio, i test impostano la variabile d'ambiente `DEBUG_DIR=./dataset/test-pdf` e salvano:
- `markdown.txt` con l'output della conversione OCR→Markdown
- `fields.txt` con i campi richiesti
- `schema_prompt.txt` con il prompt generato dal template
- `final_prompt.txt` con messaggio **system** (prompt + schema JSON) e messaggio **user** (solo markdown)
- `llm_response.txt` con la risposta grezza dell'LLM

## Endpoint principali
- `POST /api/v1/process` (protetto API key) — multipart `file=@image`
  - campi aggiuntivi: `templateName`, `prompt`, ripetere `fields[i].fieldName` e opzionale `fields[i].format` (`string|int|double|date`)
  - `X-Reasoning: think|no_think|auto`
- `GET /health`
- Swagger: `/swagger`

## Config (estratto)
Vedi `src/DocflowAi.Net.Api/appsettings.json`. Override via env (`LLM__ModelPath`, `LOG_LEVEL`, `MARKDOWNNET_VERBOSE`, ecc.).

## Smoke Test
```bash
cd smoke
./smoke.sh
```
Salva `process.json` con l'output dell'endpoint.

## agents.md
Consulta `agents.md` per l’uso con strumenti di codegen.
