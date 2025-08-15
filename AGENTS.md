# Agents (Codex) Guide

- Call `POST /api/v1/jobs?mode=immediate` with headers:
  - `X-API-Key`
  - optional `X-Reasoning: think|no_think|auto`
- Ensure a GGUF model is mounted at `/models` (compose does it).
- For local tests download `qwen2.5-0.5b-instruct-q4_0.gguf` into `./models`:
  ```bash
  export HF_TOKEN="<your token>"
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
- `rg -n -i 'pytest|:8000|sidecar|MarkitdownException|MARKITDOWN_URL|PY_MARKITDOWN_ENABLED'` should return empty
- `dotnet build -c Release`
- `dotnet test -c Release`

## Language constraints

- Frontend and backend code, including error messages, must not contain Italian words.
- All Markdown files must be in English.
- Files related to prompts and fields must remain in Italian.

## UI constraints

- The web application must be mobile-first.
- Every page must render flawlessly on mobile devices without horizontal scrolling.
- When displaying tabular data, follow Ant Design's responsive table guidelines, switching to stacked lists on small screens when needed. See: https://ant.design/components/table/#responsive
- Use clear icons to convey service health and other status information at a glance.

# Operations
The native libraries of Tesseract (libtesseract.so.5) and Leptonica (liblept.so.5) are already present in `src/MarkItDownNet/TesseractOCR/x64` and are copied automatically next to the binaries. Installing system packages or creating symbolic links is not required.

To run OCR, provide the language tessdata files and set `OcrDataPath` accordingly.

## Frontend E2E
- Ensure the environment is ready:
  ```bash
  git submodule update --init --recursive
  ./dotnet-install.sh --version 9.0.100 --install-dir "$HOME/dotnet"
  export PATH="$HOME/dotnet:$PATH"
  dotnet build -c Release
  npx playwright install
  npx playwright install-deps   # required on Linux
  ```
- Set variables in `.env`:
  - `VITE_API_BASE_URL` REST API URL (do not include the `/api/v1` prefix, the client adds it)
  - `VITE_HANGFIRE_PATH` path of the Hangfire UI (e.g. `/hangfire`)
- All calls from the frontend to REST services must use the swagger-generated client, except for health services.
- Swagger-generated files **must not be modified manually**.
- Always run and verify end-to-end tests (all must pass):
  ```bash
  npm test -- --run
  npm run build
  npm run e2e
  ```
- Playwright starts `vite preview` on port 4173: ensure the port is free.
- To use the real APIs, run the .NET API (`dotnet run`) and point `VITE_API_BASE_URL` to the running instance.
- Definition of Done: every frontend change must include appropriate E2E tests.

## Note
- `MarkItDownNet` is a git submodule; run `git submodule update --init --recursive` before the .NET build.
