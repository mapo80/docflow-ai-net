# Agents (Codex) Guide

- Call `POST /v1/jobs?mode=immediate` with headers:
  - `X-API-Key`
  - optional `X-Reasoning: think|no_think|auto`
- Ensure a GGUF model is mounted at `/models` (compose does it).
- For local tests download `qwen2.5-0.5b-instruct-q4_0.gguf` into `./models`:
  ```bash
  export HF_TOKEN="<il tuo token>"
    huggingface-cli download Qwen/Qwen2.5-0.5B-Instruct-GGUF \
      qwen2.5-0.5b-instruct-q4_0.gguf \
      --local-dir ./models --token "$HF_TOKEN"
  export LLM__ModelPath="$(pwd)/models/qwen2.5-0.5b-instruct-q4_0.gguf"
  ```
- Output is always **valid JSON** due to **GBNF grammar** at inference, then validated against **Extraction Profiles**.

Prompts:
- The server injects `/think` or `/no_think` automatically based on header/config.
- Do not add explanations; responses must be pure JSON.
## Workflow

- Initialize submodules:
  ```bash
  git submodule update --init --recursive
  ```
- Install the .NET 9 SDK locally if needed:
  ```bash
  ./dotnet-install.sh --version 9.0.100 --install-dir "$HOME/dotnet"
  export PATH="$HOME/dotnet:$PATH"
  ```
- Build and test:
  ```bash
  dotnet build -c Release
  dotnet test -c Release
  ```
- Dockerize:
  ```bash
  docker build -f deployment/Dockerfile.api -t docflow-api .
  docker run --rm -p 8080:8080 docflow-api
  ```

## MarkItDownNet
- Adapter: `src/DocflowAi.Net.Infrastructure/Markdown/MarkdownNetConverter.cs`
- Dependency Injection: `src/DocflowAi.Net.Api/Program.cs`
- Tests: `tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs`

## Pre-PR checklist
- `rg -n -i 'pytest|:8000|sidecar|MarkitdownException|MARKITDOWN_URL|PY_MARKITDOWN_ENABLED'` → **vuoto**
- `dotnet build -c Release`
- `dotnet test -c Release`

# Operations
Le librerie native di Tesseract (libtesseract.so.5) e Leptonica (liblept.so.5) sono già presenti in src/MarkItDownNet/TesseractOCR/x64 e vengono copiate automaticamente accanto ai binari. Non è quindi necessario installare pacchetti di sistema o creare collegamenti simbolici.

Per eseguire l'OCR è necessario fornire i file tessdata delle lingue e indicarli tramite OcrDataPath.

## Frontend E2E
- Imposta le variabili in `.env`:
  - `VITE_API_BASE_URL` URL dell'API REST
  - `VITE_HANGFIRE_PATH` percorso dell'interfaccia Hangfire (es. `/hangfire`)
- Per eseguire i test end-to-end:
  ```bash
  npm test -- --run
  npm run build
  npm run e2e
  ```
- Playwright avvia `vite preview` sulla porta 4173: assicurarsi che la porta sia libera.
- Per usare le API reali avviare l'API .NET (`dotnet run`) e puntare `VITE_API_BASE_URL` all'istanza in esecuzione.

## Note
- `MarkItDownNet` è un submodule git; eseguire `git submodule update --init --recursive` prima della build .NET.
