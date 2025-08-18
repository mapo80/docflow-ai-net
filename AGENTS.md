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
- `npm test -- --run`
- `npm run build`
- Unit tests for new or modified code must cover at least 90% of the affected code.
- End-to-end tests are archived under `donotrun` and must not be executed or extended.

## Persistence guidelines
- Use Entity Framework Core with a code-first approach.
- Support multiple database providers configured via `JobQueue:Database` in `appsettings`.
- Follow repository and unit of work patterns.
- All queries must reside in repositories; services may only depend on repositories and the unit of work and must not access `DbContext` directly.
- Repository queries must execute on the database server; avoid client-side evaluation.
- Any query that cannot be translated server side must be documented in `docs/query-translation-report.md`.

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

## Frontend Testing
- End-to-end tests are disabled and stored under `donotrun/`.
- Do not run or extend them.
- Run frontend unit tests and build:
  ```bash
  npm test -- --run
  npm run build
  ```
- Unit tests for new or modified code must cover at least 90% of the affected code.

## Note
- `MarkItDownNet` is a git submodule; run `git submodule update --init --recursive` before the .NET build.
