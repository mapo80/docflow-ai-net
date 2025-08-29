# Agents Playbook (codex)

Questo file guida un agente a buildare il BE/FE, eseguire i test unitari ed E2E.

## Requisiti
- .NET 9 SDK
- Node.js 18+
- pnpm o npm
- Playwright (`npx playwright install`)

## Backend (API + Worker)
```bash
cd backend
dotnet restore

# Avvio in due terminali
dotnet run --project src/DocflowRules.Worker
dotnet run --project src/DocflowRules.Api
```

### Test BE
```bash
cd backend/tests/DocflowRules.Tests
dotnet test -v n
```

## Frontend
```bash
cd frontend
npm i
npm run --workspace=packages/rules-client gen
```

### Unit test FE (UI)
```bash
cd frontend/packages/rules-ui
npm i
npx vitest run
```

### E2E (example app)
```bash
cd frontend/examples/app
npm i
npx playwright install
npm run e2e
```

## Variabili utili
- `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`, `VITE_OIDC_REDIRECT_URI`
- `OTEL_EXPORTER_OTLP_ENDPOINT` per tracing/metrics
- `Testing:MaxParallelism` (API) o body `maxParallelism` negli endpoint run


### E2E con bearer finto
Il test Playwright inserisce `localStorage.FAKE_BEARER = 'e2e-token'` prima della navigazione. L’`AuthProvider` usa quel bearer, così le chiamate REST/WebSocket includono `Authorization: Bearer e2e-token`.


## Modalità LLM locale (LlamaSharp)
```bash
# Download GGUF (es. Qwen3 0.8B - q4_k_m)
export LLM__Provider=LlamaSharp
export LLM__Local__ModelPath=/models/qwen3-0.8b-q4_k_m.gguf
export LLM__Local__Threads=$(nproc)
export LLM__Local__ContextSize=4096
export LLM__Local__MaxTokens=2048
dotnet run --project backend/src/DocflowRules.Api
```
Verifica nei log il caricamento del modello. I trace OTEL useranno `ActivitySource("DocflowRules.LLM")`.
