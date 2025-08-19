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
  export MODELS_DIR="$(pwd)/models"
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

## Testing guidelines
- Integration tests in `tests/DocflowAi.Net.Tests.Integration` must be executed only when explicitly requested.
- Frontend E2E tests are archived under `frontend/donotrun`. Do not implement or execute them, and never invoke Playwright commands (e.g., `npx playwright test`) unless explicitly requested.
- Run frontend unit tests with `npm test -- --run` and build with `npm run build`.
- Unit tests for new features or modifications must cover at least **90%** of the affected code.
- All calls from the frontend to REST services must use the swagger-generated client, except for health services.
- Swagger-generated files **must not be modified manually**.
## Updating frontend REST clients
1. Start the API server:
   ```bash
   dotnet build -c Release
   dotnet run -c Release --project src/DocflowAi.Net.Api
   ```
   The server must be reachable at `http://localhost:8080`.

2. Download the Swagger specification:
   ```bash
   cd frontend
   mkdir -p swagger/v1
   curl http://localhost:8080/swagger/v1/swagger.json -o swagger/v1/swagger.json
   ```

3. Generate the client:
   ```bash
   npm run gen:api
   ```

4. Commit the updates:
   Commit `frontend/swagger/v1/swagger.json` and the regenerated files in `frontend/src/generated`.

## Note
- `MarkItDownNet` is a git submodule; run `git submodule update --init --recursive` before the .NET build.
