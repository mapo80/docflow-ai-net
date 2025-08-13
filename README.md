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

## Quickstart
```bash
git submodule update --init --recursive
./dotnet-install.sh --version 9.0.100 --install-dir "$HOME/dotnet"
export PATH="$HOME/dotnet:$PATH"
dotnet restore && dotnet build -c Release && dotnet test -c Release
```

## Avvio rapido con Docker Compose
```bash
cd deployment
docker compose up --build
# API: http://localhost:5214
```
Snippet minimale:
```yaml
services:
  api:
    build:
      context: ..
      dockerfile: ./deployment/Dockerfile.api
    ports:
      - "5214:8080"
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

## Endpoint principali
- `POST /api/v1/process` (protetto API key) — multipart `file=@image`
  - campi aggiuntivi: `templateName`, `prompt`, ripetere `fields[i].fieldName` e opzionale `fields[i].format` (`string|int|double|date`)
  - `X-Reasoning: think|no_think|auto`
- `GET /health`
- Swagger: `/swagger`

## Config (estratto)
Vedi `src/DocflowAi.Net.Api/appsettings.json`. Override via env (`LLM__ModelPath`, `LOG_LEVEL`, `MARKDOWNNET_VERBOSE`, ecc.).

## Bounding boxes
- coordinate in **pixel**: `X`, `Y`, `Width`, `Height`
- coordinate **normalizzate** \[0..1]: `XNorm`, `YNorm`, `WidthNorm`, `HeightNorm`
- formato: **xywh** con origine in alto a sinistra

### Bounding Box Evidence
Il resolver `DocflowAi.Net.BBoxResolver` ancora ogni campo dell'LLM alle parole del documento e restituisce:

```json
{
  "Key": "amount",
  "Value": "1234.56",
  "Confidence": 0.92,
  "Evidence": [
    {
      "Page": 0,
      "WordIndices": [5,6],
      "BBox": [0.1,0.2,0.3,0.05],
      "Text": "1234.56",
      "Score": 0.98
    }
  ]
}
```

Le coordinate sono normalizzate [0..1] con origine in alto a sinistra. L'algoritmo di distanza può essere scelto tramite configurazione `BBox:DistanceAlgorithm` oppure variabile d'ambiente `BBox__DistanceAlgorithm`.

### Resolver strategy
Il resolver supporta le strategie **Legacy** e **TokenFirst** (predefinita). Seleziona la strategia con `Resolver:Strategy` oppure variabile d'ambiente `Resolver__Strategy`.

```json
"Resolver": {
  "Strategy": "TokenFirst",
  "TokenFirst": {
    "DistanceAlgorithm": "BitParallel",
    "EditDistanceThreshold": 0.25,
    "AdaptiveShortMax": 0.40,
    "AdaptiveLongMax": 0.35,
    "MaxCandidates": 10,
    "EnableLabelProximity": true
  }
}
```

## Smoke Test
```bash
cd smoke
./smoke.sh
```
Salva `process.json` con l'output dell'endpoint.

## Test di Integrazione MarkItDownNet
Per i risultati dell'estrazione su PDF e PNG consulta [dataset/markitdownnet-integration.md](dataset/markitdownnet-integration.md).

## Test di Integrazione BBoxResolver
Esecuzioni della strategia **TokenFirst** su PDF e PNG con confronto tra `BitParallel` e `ClassicLevenshtein` sono documentate in [dataset/boxsolver-integration.md](dataset/boxsolver-integration.md).

## agents.md
Consulta `agents.md` per l’uso con strumenti di codegen.
