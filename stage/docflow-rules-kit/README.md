# Docflow Rules Kit — SDK pluggabile (FE + BE)

## Backend
- `DocflowRules.Sdk` (NuGet-ready): runner Roslyn, `ExtractionContext`, `ScriptGlobals`, DI `AddDocflowRulesCore()`
- `DocflowRules.Storage.EF` (NuGet-ready): entità, DbContext, repository, seed, DI `AddDocflowRulesSqlite(...)`
- `DocflowRules.Api`: host REST dimostrativo (riutilizzabile o copiabile)

## Frontend
- `@docflow/rules-client`: SDK HTTP/TypeScript (REST client)
- `@docflow/rules-ui`: libreria React con **RulesEditor** e **RuleTestsPanel** (Monaco + AntD)
- `examples/app`: esempio Vite React che consuma i package

### Avvio rapido
Backend:
```bash
cd backend
dotnet restore
dotnet run --project src/DocflowRules.Api
```
Frontend example:
```bash
cd frontend
npm i
npm run --workspace=examples/app dev
```
Apri http://localhost:5173 (proxy /api → 5080)

### Integrare in altre soluzioni
- **BE**: referenzia `DocflowRules.Sdk` e, se vuoi SQLite out-of-the-box, `DocflowRules.Storage.EF` e chiama:
  ```csharp
  services.AddDocflowRulesCore();
  services.AddDocflowRulesSqlite("Data Source=rules.db");
  ```
- **FE**: installa i pacchetti (o copia la cartella) e usa:
  ```ts
  import { HttpRulesClient } from '@docflow/rules-client'
  import { RulesEditor, RuleTestsPanel } from '@docflow/rules-ui'
  ```

### TODO principali
- Bridge **LSP** per IntelliSense C# in Monaco (OmniSharp / Roslyn LSP via WS)
- Runner **out-of-process** (sicurezza) + limiter risorse
- Pipeline di **versioning** (Draft/Staged/Published) e firma built-in
- UI Test runner avanzato (diff visivo per campo)
- Pacchettizzazione npm/nuget (script build e CI)


## Generazione client FE da Swagger
Avvia prima il backend (Swagger su `/swagger/v1/swagger.json`), poi genera il client TypeScript:
```bash
cd frontend
npm run --workspace=packages/rules-client gen
```
Il pacchetto `@docflow/rules-client` usa **openapi-typescript** + **openapi-fetch** (client moderno basato su `fetch`).


## Novità in questa versione
- **IntelliSense C# in Monaco (LSP)**: backend espone `/lsp/csharp` come ponte WebSocket verso un Language Server esterno (config in `backend/src/DocflowRules.Api/appsettings.json`). Il FE (`@docflow/rules-ui`) può collegarsi passando `lspUrl` a `<RulesEditor/>`.
- **Runner out-of-process**: nuovo servizio `DocflowRules.Worker` (porta 5095) esegue compile/run; l'API usa il client HTTP se `Runner:Mode=OutOfProcess`.

### Avvio LSP
1. Installa un server LSP C# (es. OmniSharp con flag `-lsp`).
2. Aggiorna `appsettings.json > Lsp.ServerPath` e `Args`.
3. Avvia il backend; l'endpoint LSP è `ws://localhost:5080/lsp/csharp`.

### Avvio Runner out-of-process
In una seconda shell:
```bash
cd backend
dotnet run --project src/DocflowRules.Worker
```
L'API userà il worker (porta 5095) per eseguire i script.


### API Key
- **Backend**: configura le chiavi in `backend/src/DocflowRules.Api/appsettings.json` → `Auth:ApiKeys` e in `backend/src/DocflowRules.Worker/appsettings.json` → `Auth:WorkerKeys`.
  - Override via env: `Auth__ApiKeys__0=xxx`, `Auth__WorkerKeys__0=yyy`.
- **Frontend**: passa la chiave tramite `VITE_API_KEY` (dev). Il client FE invia `X-API-Key` e il WS LSP usa `?api_key=` nella URL.

## Hardening & Deploy
- **Docker**: `docker-compose.yml` con limiti CPU/Mem, `HEALTHCHECK`, `seccomp` e profili AppArmor d'esempio.
- **Kubernetes**: manifest completi con `resources`, `liveness/readiness`, `seccompProfile: RuntimeDefault`, drop capabilities.
- **Retry**: client API→Worker con retry/backoff (Polly).
- **LSP workspace**: il bridge crea una *workspace dir* per sessione (query `workspaceId`) e avvia il server con quella `WorkingDirectory`.


## Test Runner Pro (UI)
- Tabella con **filtri** (All/Passed/Failed/Not run), **search**, **row selection**.
- Azioni: **Run All**, **Run Selected**, **Re-run Failed**.
- Pannello espandibile per ogni test con **diff**, **logs**, **error**.
- Endpoint BE: `POST /api/rules/{id}/tests/run-selected`.


### Test Runner Pro — Diff visivo & tagging
- **Diff per campo**: nella riga espansa vedi una tabella con Field, Rule (equals/approx/regex/exists), Expected, Actual, Tolerance, Status (**pass/fail**).
- **Tagging e Suite**: test con `suite`, `tags[]`, `priority (1-5)`; filtri per Suite/Tags; editor metadata (drawer) e supporto in creazione test.
- Backend: esteso `RuleTestCase` con `Suite`, `TagsCsv`, `Priority`; endpoint `PUT /api/rules/{ruleId}/tests/{testId}` per aggiornare metadata; run restituisce anche `id` e `actual`.


## Nuove funzionalità enterprise
- **EF Core Migrations**: `DocflowRules.Storage.EF/Migrations` con **InitialCreate**. All'avvio l'API applica `Database.Migrate()` e poi esegue il seed.
- **Suite/Tag management**: endpoint CRUD `/api/suites` e `/api/tags`, UI in **Example App → Taxonomies** con colori/descrizioni.
- **Parallel testing**: esecuzione test in parallelo con **limite configurabile** (`Testing:MaxParallelism`, default 4).
- **Coverage**: `GET /api/rules/{id}/tests/coverage` restituisce mappa `{field, tested, mutated, hits, pass}`; UI: bottone **Compute Coverage** nel pannello test.
- **Logging intenso**: Serilog con `UseSerilogRequestLogging()` e log mirati in controller/worker.

### Nota migrazioni
Se avevi un DB creato con `EnsureCreated`, cancella `backend/src/DocflowRules.Api/bin/.../data/rules.db` prima di riavviare, così verrà ricreato con lo schema a migrazioni.


## Osservabilità & Concurrency dal FE
- **OpenTelemetry** (API + Worker): tracing (AspNetCore + HttpClient) e metrics (Runtime incluse). Export **OTLP** (set `OTEL_EXPORTER_OTLP_ENDPOINT`) e Console exporter attivi di default.
- **Serilog**: arricchito con Machine/Process/Thread; log strutturati su **Console**.
- **Activity & Metrics custom**: `RuleTestsController` aggiunge spans e misura `test.duration.ms`, counters pass/fail.
- **Parallelismo dal FE**: l'UI consente di scegliere la concurrency; gli endpoint `/tests/run` e `/tests/run-selected` ricevono `maxParallelism` nel body e sovrascrivono la config.


### Coverage Heatmap
- Nuova **dashboard heatmap** (aprila dal pulsante *Heatmap* nel pannello Unit Tests).
- Mostra una matrice **Test × Field** con colori: *pass* (verde), *fail* (rosso), *not run* (giallo), *not asserted* (grigio).
- Include **percentuali per riga e colonna**, **legend**, e un bottone **Run for Heatmap** con scelta **Concurrency**.


## Sicurezza: OAuth/OIDC + RBAC
- API configurata con **JWT Bearer** (Authority/Audience da `appsettings.json` o env).
- Ruoli: `viewer` (lettura), `editor` (crea/modifica), `reviewer` (stage), `admin` (publish/delete).
- FE: integrazione **OIDC** con `oidc-client-ts`. Il client REST usa **Bearer**.

## Governance & Versioning
- Regole con `Status` (Draft/Staged/Published), `SemVersion`, `Signature`, `BuiltinLocked`.
- Endpoint: `POST /api/rules/{id}/stage` (reviewer), `POST /api/rules/{id}/publish` (admin).

## LSP stabile
- Workspace **csproj** con riferimento a `DocflowRules.Sdk.dll` (copiato nel workspace).
- Endpoint **sync**: `POST /lsp/workspace/sync?workspaceId=...` body `{ filePath, content }`.

## Ricerca / paginazione / ordinamento
- `/api/rules?search=&sortBy=&sortDir=&page=&pageSize=`
- `/api/rules/{id}/tests?search=&suite=&tag=&sortBy=&sortDir=&page=&pageSize=`

## Validazioni & errori
- **FluentValidation** e middleware `UseProblemDetails()` restituiscono **ProblemDetails** coerenti per errori e validazioni.


## Identity (UI) & LSP Sync
- **Admin UI**: gestione **utenti/ruoli** (crea/elimina/ruoli) sotto *Admin* nell'app esempio. Backend locale con tabelle `Users`/`UserRoles` (sostituibile con IdP SCIM/Keycloak in futuro).
- **Debounced LSP sync**: l'editor invia il contenuto a `/lsp/workspace/sync` con debounce **500ms** (headers auth opzionali via prop).

## Validazioni estese
- **Tests**: nome obbligatorio, `expect.fields` oggetto, priorità 1..5.
- **Suite/Tag**: nome obbligatorio, colore validato (#rrggbb), messaggi **user-friendly**.
- Errori coerenti via **ProblemDetails**.


## Validazioni avanzate `expect.fields`
Le regole supportate **per campo**:
- `equals`: qualsiasi JSON; verifica di uguaglianza
- `approx`: numero o oggetto `{ value: number, tol?: number }`. È possibile anche `tol` a livello campo (stesso effetto).
- `regex`: stringa regex valida (.NET)
- `exists`: booleano

Errori restituiti come **ProblemDetails** con messaggi chiari in italiano, ad esempio:
- `expect.fields.amount.approx`: *La regola 'approx' richiede un numero…*
- `expect.fields.code.regex`: *Regex non valida: …*
- Chiavi ignote vengono rifiutate con messaggio *Chiave sconosciuta*.

### Paginazione/ordinamento/ricerca test
Endpoint: `GET /api/rules/{id}/tests?search=&suite=&tag=&sortBy=name|updatedAt&sortDir=asc|desc&page=&pageSize=`  
La **UI** propaga `sortBy/sortDir` cliccando sulle intestazioni (Name, Updated).

### Editor Sync status
L’editor mostra **badge**: *saving…* durante il debounce e *synced* al successo del salvataggio su `/lsp/workspace/sync`.

## Test
### Backend (.NET, xUnit)
Esegui:
```bash
cd backend/tests/DocflowRules.Tests
dotnet test
```
Copertura ricca per il validator con decine di casi (regex valide/invalidi, approx con/ senza tol, exists, chiavi ignote, nessuna regola, casi minimi validi).

### Frontend (unit, Vitest)
```bash
cd frontend/packages/rules-ui
npm i
npx vitest run
```

### Frontend (E2E, Playwright)
```bash
cd frontend/examples/app
npm i
npx playwright install
npm run e2e
```
I test e2e **mockano le API** principali (rules, tests, suites, tags) per essere riproducibili.



## Ordinamenti avanzati (multi-sort) & stato in URL
- Endpoint test supporta `sort=name:asc,priority:desc,updatedAt:desc` oltre a `sortBy/sortDir` legacy.
- UI: puoi ordinare più colonne (Name, Suite, Tags, Prio, Updated). Lo stato di **ricerca/pagina/sort** può essere **persistito in URL** (attivato nell'esempio).

## Regole di confronto runtime
Il confronto `Compare()` lato API è stato esteso a:
- `exists` (true/false)
- `regex` con controllo pattern
- `approx` con normalizzazione `{ value, tol }` o `approx:number` + `tol:number`
- `equals` (match JSON esatto)
Le difformità aggiungono al `diff` dettagli `field`, `error`, `expected`, `actual`.

## Heatmap drill‑down & export
- Clic su una cella → **drill‑down** con esito.
- Pulsante **Export Coverage CSV** per scaricare la matrice (Test×Field).

## E2E e OIDC fake
- L’e2e imposta `localStorage.FAKE_BEARER` per bypassare l’OIDC reale.


## Stato persistente in URL (filtri suite/tag) + multi‑filtro tag (AND/OR)
- La pagina **Unit Tests** ora salva/riprende automaticamente nell’URL:
  - `t.search`, `t.page`, `t.sort`, **`t.suite`**, **`t.tags`**, **`t.tagsMode`**.
- Endpoint `GET /api/rules/{id}/tests` accetta ora:
  - `tags` (lista separata da virgole) e `tagsMode=and|or` oltre a `tag` singolo.
- UI: selezione **multi-tag** (Select) e toggle **AND/OR** (Segmented). La tabella ricarica automaticamente i dati.


## AI Suggestions (V1)
Genera automaticamente una **rosa di test** a partire dalla funzione:

- **Analisi statica** (heuristic): soglie numeriche, regex, controlli di esistenza → skeleton test.
- **LLM rifinitore** (facoltativo): migliora nomi/regex/tolleranze (provider mock di default).
- **Validazione** con `TestUpsertValidator`, **dedupe** e **ranking** (coverage delta + varietà).
- **Coverage delta** calcolato rispetto ai test esistenti della regola.

### API
- `POST /api/ai/tests/suggest?ruleId=...`
  ```json
  { "userPrompt": "opzionale", "budget": 20, "temperature": 0.2 }
  ```
  → restituisce `{ suggestions: [{ id, reason, score, coverageDelta, payload }], model, totalSkeletons }`
- `POST /api/ai/tests/import?ruleId=...`
  ```json
  { "ids": ["..."], "suite": "ai", "tags": ["ai"] }
  ```

### UI
Nel pannello **Unit Tests** → sezione **AI Suggestions**:
- Prompt opzionale, **Budget**, **Temperature**.
- Lista suggerimenti con **reason**, **coverage Δ**, anteprima JSON.
- Seleziona e **Importa**: crea i test reali con suite/tag `ai`.

### Provider LLM
- Default: `MockLLMProvider` (eco).  
- Per produzione, registra un provider custom (OpenAI/Azure/Ollama) sostituendo `ILLMProvider` in DI.
  - Config consigliata: `LLM_PROVIDER`, `LLM_MODEL`, `LLM_API_KEY`.


### Provider LLM (OpenAI) con metriche
Per usare OpenAI al posto del provider mock:
```jsonc
// backend/src/DocflowRules.Api/appsettings.json
"LLM": {
  "Provider": "OpenAI",
  "ApiKey": "sk-...",
  "Model": "gpt-4o-mini",
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "InputCostPer1K": "0.0005",
  "OutputCostPer1K": "0.0015"
}
```
Variabili env equivalenti (`LLM__Provider`, `LLM__ApiKey`, ecc.) sono supportate.

**Metriche**: la risposta di `/api/ai/tests/suggest` include `inputTokens`, `outputTokens`, `durationMs`, `costUsd`. La UI mostra un riepilogo (modello, token, durata, costo).

> Nota: l’endpoint usa il formato Chat Completions; puoi adattare facilmente al **Responses API** sostituendo l’URL e il payload.


### Retry/Backoff & Tracing (OTEL) per LLM
Il provider **OpenAI** ora include:
- **retry con backoff esponenziale + jitter** (config `LLM:Retry:*`),
- **tracing OTEL** con `ActivitySource("DocflowRules.LLM")`,
- **metriche**: `llm.calls`, `llm.errors`, `llm.input_tokens`, `llm.output_tokens`, `llm.latency.ms`, `llm.cost.usd`.

Configurazione esempio (`appsettings.json`):
```jsonc
"LLM": {
  "Provider": "OpenAI",
  "ApiKey": "sk-...",
  "Model": "gpt-4o-mini",
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "InputCostPer1K": "0.0005",
  "OutputCostPer1K": "0.0015",
  "Retry": { "MaxAttempts": "3", "BaseDelayMs": "200", "MaxDelayMs": "4000", "JitterMs": "250" }
}
```

I **trace** includono tag utili (model, endpoint, duration, tokens, cost, esito) e **eventi** `attempt`, `retry`, `error`. Le **metriche** possono essere esportate via l'exporter che hai già configurato in `Program.cs`.


## LLM locale con LlamaSharp (CPU, GGUF)
Per eliminare i costi cloud puoi usare un modello **GGUF** con **LlamaSharp** (backend CPU).
Consigliato (per CPU): modello compatto e velocissimo, es. **Qwen3-Zero-Coder-Reasoning V2 0.8B NEO-EX (GGUF)**.

### Setup
1. Scarica il file `.gguf` (es. `*q4_k_m.gguf`) da Hugging Face.
2. Imposta il percorso nel backend:
   ```bash
   export LLM__Provider=LlamaSharp
   export LLM__Local__ModelPath=/percorso/al/modello/qwen3-0.8b-q4_k_m.gguf
   export LLM__Local__Threads=$(nproc)     # opzionale
   export LLM__Local__ContextSize=4096     # opzionale
   export LLM__Local__MaxTokens=2048       # opzionale
   ```
3. Avvia le API e usa la UI **AI Suggestions** normalmente.

> Nota: token usage/costi non sono disponibili con certezza in locale → la UI mostrerà 0; la **durata** (ms) viene tracciata via OTEL.

### Performance tips (CPU)
- Scegli quantizzazione **q4_k_m** per un buon trade-off velocità/qualità su CPU.
- Imposta `Threads` ≈ numero core fisici.
- `ContextSize` 4096 è un buon default; alzarlo aumenta RAM e latenza.
- Assicurati che il processo abbia **AVX2** abilitato (la maggior parte delle CPU moderne).

### Switch rapido tra provider
- `LLM:Provider=OpenAI` → cloud.
- `LLM:Provider=LlamaSharp` (o `Local`) → on-prem (GGUF).
- `LLM:Provider=Mock` → nessuna chiamata esterna (eco degli skeleton).



## Gestione modelli GGUF (download da Hugging Face)
- Configura la cartella dei modelli in `appsettings.json` → `LLM:Local:ModelsDir` (default `models/`).  
- Con ruolo **admin**, vai su **Admin → LLM**:
  - Nel form dei modelli con provider **LlamaSharp**, il campo “Model GGUF” è una **tendina** popolata con i file `.gguf` **già scaricati**.
  - Puoi **scaricare** nuovi modelli da Hugging Face (repo + filename + revision) → il backend li scarica nella cartella configurata.
  - Hai una barra di **progresso**; al termine il file appare nella tendina.

### API
- `POST /api/admin/gguf/download` `{ repo, file, revision? }` → crea un **job** di download (accetta e parte in background).
- `GET /api/admin/gguf/jobs/{id}` → stato del job.
- `GET /api/admin/gguf/available` → elenco dei `.gguf` disponibili (nome, path, size, modified).
- Auth: policy `admin`.

> Imposta (se necessario) `HF:Token` per repo privati (header `Authorization: Bearer ...`).



### Cancellazione GGUF
- **UI**: nel form modello LlamaSharp, dopo aver selezionato un file GGUF, clicca **Elimina GGUF**.
- **API**: `DELETE /api/admin/gguf/available` body `{ "path": "/abs/path/modello.gguf" }`
  - Sicurezza: è possibile cancellare **solo** file dentro `LLM:Local:ModelsDir`.
  - Se il file è **in uso** da un modello **abilitato**, la cancellazione viene rifiutata.


## Test
### Backend (xUnit)
```bash
cd backend/tests/DocflowRules.Api.Tests
dotnet test
```
Include test per `GgufService.DeleteAvailableAsync`:
- OK quando il file non è in uso
- Bloccato se **referenziato da modello abilitato**
- Bloccato se **modello attivo** punta al file
- Bloccato se il percorso è **fuori** da `LLM:Local:ModelsDir`

### Frontend Unit (Vitest)
```bash
cd frontend/examples/app
npm run test
```

### Frontend E2E (Playwright, con mock API)
```bash
cd frontend/examples/app
npx playwright install
npm run test:e2e
```
Il test visita `/admin/llm`, apre la tab **GGUF Files**, verifica la riga ed effettua una cancellazione simulata.


### Coverage
- **Backend (.NET)**
  ```bash
  cd backend/tests/DocflowRules.Api.Tests
  dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line
  ```
- **Frontend (Vitest)**
  ```bash
  cd frontend/examples/app
  npm run test
  # report in frontend/examples/app/coverage; soglie al 80% (statements/branches/functions/lines)
  ```


> **Soglia di coverage**: test configurati per richiedere **≥ 80%** su FE (Vitest) e BE (Coverlet). I nuovi test coprono servizi chiave (analyzer, suggestion, LLM providers, GGUF service) e UI principali (Admin LLM, AI Suggestions).


## Database multiprovider (solo SQLite per ora)
Configura da `appsettings.json` o ENV:
```jsonc
"Database": {
  "Provider": "sqlite",                     // in futuro: postgres, sqlserver
  "ConnectionString": "Data Source=docflow.db"
}
```
Vars equivalenti:
- `Database__Provider=sqlite`
- `Database__ConnectionString="Data Source=docflow.db"`
Oppure usa `ConnectionStrings:db` come già supportato.

## Seed utente admin
All'avvio, un hosted service crea (se mancante) un utente **admin** e assegna i ruoli.
Config:
```jsonc
"Seed": {
  "Admin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Roles": "admin,editor,reviewer,viewer"
  }
}
```
ENV:
- `Seed__Admin__Username`, `Seed__Admin__Email`, `Seed__Admin__Roles`

> NB: il seed popola **AppUser** e **AppUserRole**. L'autenticazione resta OIDC/JWT lato API; la UI demo usa uno stub per semplificare i test.


## Database: multi-provider (per ora solo Sqlite)
Configura il provider e la connection string in `appsettings.json`:
```jsonc
"Database": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=docflow.db"
}
```
Override via env:
```
export Database__Provider=Sqlite
export Database__ConnectionString="Data Source=docflow.db"
```
> In questa build è supportato **solo Sqlite**. La struttura è pronta per Postgres/SQL Server: basta aggiungere il package EF Core relativo e il ramo `UseNpgsql/UseSqlServer`.

### Seeding utente admin
Abilitato di default:
```jsonc
"Seed": { "Admin": { "Enabled": true, "Username": "admin", "Email": "admin@local" } }
```
All'avvio (dopo la migration) viene creato l'utente `admin` con ruolo **admin** se non esiste già.
Disattiva con `Seed:Admin:Enabled=false`.


### Password admin (seed)
La seed crea/aggiorna l'utente **admin** e imposta una password **hashata (BCrypt)**.  
Configura da `appsettings.json` o ENV:
```jsonc
"Seed": {
  "Admin": {
    "Enabled": true,
    "Username": "admin",
    "Email": "admin@local",
    "Password": "changeme!"
  }
}
```
> Nota: l'autenticazione attuale usa OIDC/JWT esterno; questa password è pensata per la **modalità local** o per future estensioni di login locale.


### Logout e gestione errori FE
- **Logout (Local)** e **Sign out (OIDC)** disponibili nel menu **Account**.
- **Gestione errori centralizzata**: lo SDK FE espone `setGlobalErrorHandler`; l'app registra un handler che mostra un **toast** (`message.error`) con il messaggio proveniente dal backend.


## Clonazione rapida
- **Regola**: `POST /api/rules/{id}/clone` body `{ newName?, includeTests? }`
- **Test**: `POST /api/rules/{ruleId}/tests/{testId}/clone` body `{ newName?, suite?, tags? }`
- **Suite**: `POST /api/suites/{id}/clone` body `{ newName }`
- UI: pagina **Rules** con azione **Clone + tests**; in griglie test/suites pulsanti clone (dove applicabile).

## No-code Rule Builder
- Pagina **Builder** con blocchi: **exists**, **compare**, **regex**, **set**.
- Backend `POST /api/rulebuilder/compile` genera **C# (script)** preview; da UI **Create Rule** salva la regola e porta all’editor.

## Property-based & Fuzz testing
- Backend **FuzzService**: genera input boundary/mancanti per ogni campo rilevato via StaticAnalyzer.
- API: `POST /api/rules/{ruleId}/fuzz/preview` → `{ items[] }`; `POST /api/rules/{ruleId}/fuzz/import` → `{ imported }`.
- UI: nel pannello test compare bottone **Fuzz (preview)**; puoi poi importare come per le AI Suggestions.


### Property Report (UI) & Import Failure → Test
- **UI**: in **Rule Edit** c'è la card **Property Report** con `Trials/Seed`, esecuzione e tabella dei **failure**.
- Puoi selezionare righe e **Importare come test** (suite `property-fails`, tag `property`).

### Builder avanzato
- Nuovi blocchi: **when(cond)** e **calc(target = expr)** con validazione server-side ed emissione C#.
- `POST /api/rulebuilder/validate` copre anche *when* e *calc* con messaggi specifici.

### Arbitrary mirati
- Il Property runner ora **inferisce i tipi campo** dalla regola (confronti numerici, parsing date, regex) e genera input coerenti (`number`, `date`, `string`). Se il codice non dà indizi, ricade su stringhe.


### Property Report – grafici e import avanzato
- Grafici **Pass/Fail** (torta) e **Failures by property** (bar) nella card Property Report.
- Prima dell'import puoi scegliere **suite** e **tag** (autocomplete da tassonomie esistenti; i tag sono liberi con suggerimenti).
