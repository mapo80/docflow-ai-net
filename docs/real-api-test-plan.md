# PIANO DI TEST DI INTEGRAZIONE (REAL API) — BACKEND (C#)

Obiettivo: eseguire **test end-to-end reali** contro le API esposte (nessun mock), usando file e servizi reali. I test **non** modificano il backend e verificano il comportamento osservabile via REST.
Ambito endpoint:
- POST /v1/jobs (queued | ?mode=immediate)
- GET  /v1/jobs (lista paginata)
- GET  /v1/jobs/{id}
- DELETE /v1/jobs/{id}
- GET  /health/live
- GET  /health/ready
- (Hangfire dashboard: solo verifica raggiungibilità via URL pubblico, non API)

⚙️ **ESECUZIONE SU RICHIESTA**
- Framework: **xUnit** (o NUnit), libreria HTTP: **HttpClient**. Niente stub/mocks.
- Categoria dei test: `Trait("Category","RealE2E")` (filtrabile con `--filter`).
- Abilitazione tramite env var (esempio): `DOCFLOW_API_BASE_URL=https://<host>` e (opzionale) `DOCFLOW_HANGFIRE_PATH=/hangfire`.
- I test **saltano** automaticamente se la base URL non è impostata.

📦 **DATI & PREREQUISITI**
- File di prova (cartella `TestData/` nel progetto test):
  1) `sample.pdf` ~100–200KB (PDF valido)
  2) `sample-large.pdf` > limite `UploadLimits.MaxRequestBodyMB` (es. 20% oltre)
  3) `image.png` ~100KB (MIME consentito)
  4) `evil.exe` ~10KB (MIME/estensione non ammessi)
- Payload di esempio:
  - `prompt_text`: "Estrai intestatario, totale, iva."
  - `prompt_json`: `{ "task": "extract", "entities": ["buyer","total","vat"] }`
  - `fields_json`: `{ "entities": [{"name":"buyer","type":"string"},{"name":"total","type":"number"},{"name":"vat","type":"number"}] }`
- Utilità test:
  - Generatore `Idempotency-Key` (GUID e SHA-256 di file+prompt+fields).
  - Lettura header `Retry-After` **e** body `retry_after_seconds`.

🧪 **SUITE E CASI (Arrange → Act → Assert)**

### A) SMOKE & HEALTH
A1. **Live_OK** – GET /health/live → 200.

A2. **Ready_OK_or_Reasons** – GET /health/ready → 200 **oppure** 503 con `reasons[]` tra: `litedb_unavailable`, `data_root_not_writable`, `backpressure`. Se 503: asserire che `reasons` sia non vuoto.

A3. **Hangfire_Accessible** (facoltativo) – Se `DOCFLOW_HANGFIRE_PATH` definito: fare una **HEAD/GET** all’URL pubblico; aspettarsi 200/401/403/redirect (non deve essere 404/5xx persistente).

### B) SUBMIT — QUEUED (202)
B1. **Submit_Queued_202_Minimal** – POST /v1/jobs con `sample.pdf`, `prompt_text`, `fields_json` → 202 `{ job_id, status_url }`. Verifica che GET {id} ritorni status `Queued` o `Running`.

B2. **Submit_Queued_202_IdempotencyKey** – Ripeti lo stesso POST con header `Idempotency-Key: <K>` → 202 **stesso** `job_id`.

B3. **Submit_Queued_202_DedupeHash** – Due POST in sequenza **senza** Idempotency-Key con **stesso** file/prompt/fields → 202 **stesso** `job_id` (dedupe entro finestra).

### C) SUBMIT — IMMEDIATE (200)
C1. **Immediate_200_Success** – POST /v1/jobs?mode=immediate con `sample.pdf` + `prompt_text` + `fields_json` → 200 `{ job_id, status="Succeeded", duration_ms, result_path? }`. GET {id} → `Succeeded`. Se `result_path` pubblico: GET file → 200 (facoltativo).

C2. **Immediate_200_Failed** (se riproducibile) – Usare un input che generi failure (es. payload volutamente inconsistente) → 200 `{ status="Failed", error }`. GET {id} → `Failed`.

C3. **Immediate_429_Capacity** (se ambiente con capacità limitata) – Avvia una immediate che impegna il servizio (eseguire due POST concorrenti in Task.WhenAll). Il secondo POST → 429 con header/body `Retry-After`. Nessun job nuovo se non previsto da fallback. Se il backend ha `FallbackToQueue=true`: il secondo → 202 queued (asserire differenza).

C4. **Immediate_200_Cancelled** (opzionale) – Avvia immediate e **annulla** la richiesta client (CancellationToken) durante l’elaborazione. GET {id} → `Cancelled` (se supportato dal backend).

### D) LISTA PAGINATA — GET /v1/jobs
D1. **List_Default_Paged_Desc** – GET /v1/jobs?page=1&pageSize=20 → 200 { page, pageSize, total, items }. Verifica `items` ordinati per `createdAt` DESC (confronto `createdAt` dei primi 3).

D2. **List_Pagination_NextPage** – Se `total` > pageSize, GET page=2 → elementi diversi e coerenza col totale.

D3. **List_Filters_ClientSide** (solo FE) – (Il BE non espone filtri dedicati) — validare localmente che il subset filtrato da FE corrisponda ai `status` attesi (non serve chiamata extra al BE).

### E) DETTAGLIO — GET /v1/jobs/{id}
E1. **Get_ById_Existing** – Dal job creato in B1: GET {id} → 200; assert: campi base (status, derivedStatus, progress, timestamps). Se paths presenti: **non** dedurre stato dai file, ma verificare solo la **presenza** dei path.

E2. **Get_ById_NotFound** – GET guid random → 404 `{ error:"not_found" }`.

### F) CANCEL — DELETE /v1/jobs/{id}
F1. **Cancel_Queued_202** – Su un job in `Queued`: DELETE → 202, GET {id} → `Cancelled`.

F2. **Cancel_Running_202** (se possibile) – Se un job è `Running`: DELETE → 202, GET {id} → `Cancelled` (o `Running` per breve, poi `Cancelled`).

F3. **Cancel_Terminal_409** – Su job `Succeeded` o `Failed`: DELETE → 409 `{ error:"conflict" }`.

### G) BACKPRESSURE & RATE LIMIT (429)
G1. **QueueFull_429_SubmitQueued** – Precondizione: coda piena (eseguire N submit finché GET /health/ready riporta reason `backpressure` oppure fino a ottenere 429). Nuovo POST queued → 429 `{ error:"queue_full" }` + header/body `Retry-After`.

G2. **ImmediateCapacity_429_SubmitImmediate** – (Come C3) Due immediate in parallelo → uno dei due 429 `{ error:"immediate_capacity" }` + `Retry-After` (se backend senza fallback).

G3. **RateLimited_429** (se policy attiva) – Invia >N submit in finestra breve → 429 `{ error:"rate_limited" }` + `Retry-After`.

### H) ERRORI PAYLOAD & VALIDAZIONE
H1. **PayloadTooLarge_413** – POST queued con `sample-large.pdf` → 413 `{ error:"payload_too_large" }`.

H2. **MimeNotAllowed_400** – POST con `evil.exe` (octet-stream) → 400 `{ error:"bad_request" }`.

H3. **BadJson_400** – `fields` o `prompt` JSON **non valido** → 400.

H4. **InsufficientStorage_507** (se riproducibile) – (Solo se ambiente predisposto) POST che attivi 507 → 507 `{ error:"insufficient_storage" }`.

### I) CONSISTENZA, IDP & DEDUPE
I1. **IdempotencyKey_Reused_AfterRestart** (se riavvio possibile) – Submit con stessa `Idempotency-Key` **prima e dopo** un riavvio del servizio → stesso `job_id`.

I2. **HashDedupe_Window** – Due submit identici a distanza < TTL dedupe → stesso `job_id`; a distanza > TTL → job diverso.

### L) ARTEFATTI (paths.*)
L1. **OutputJson_Available_OnSuccess** (se esposto) – Per job `Succeeded`: prova HTTP GET al `paths.output` (se pubblico) → 200 e `application/json`.

L2. **ErrorTxt_Available_OnFailure** (se esposto) – Per job `Failed`: HTTP GET `paths.error` → 200 e `text/plain`.

🧼 **CLEAN-UP**
- I job **terminali** restano in sistema (TTL cleanup notturno); non è previsto “delete hard” via API.
- I test generano **prefissi** nei prompt (es. `"[ITEST-<RunId>] ..."`) per facilitarne il riconoscimento in manutenzione.
- I job `Queued/Running` creati dai test vanno cancellati con DELETE (F1/F2).

📊 **OUTPUT & LOGGING**
- Ogni test logga: `RequestId` (se header), `JobId`, tempi, esito, eventuale `Retry-After`.
- Al termine produrre un **report JUnit/TRX** e un CSV con: TestId, JobId, Stato finale, Durata, Error.

▶️ **ESECUZIONE (esempio)**
- Precondizioni: backend raggiungibile con servizi reali.
- Comandi:
  - `set DOCFLOW_API_BASE_URL=https://host:port` (o export su Linux/Mac)
  - `dotnet test -c Release --filter Category=RealE2E`
  - (facoltativo) `--logger "trx;LogFileName=real-e2e.trx"`

✅ **CRITERI DI ACCETTAZIONE**
- Tutti i casi A–H **passano**. I casi opzionali (C4, F2, G3, H4, I1) passano se l’ambiente li rende riproducibili.
- Per i 429, è presente `Retry-After` **in header o body**.
- Immediate vs queued: **200** (immediate) e **202** (queued) rispettati; GET {id} coerente con stato terminale.
- Health: `/health/live` 200; `/health/ready` → 200 o 503 con reasons non vuote.
- Nessuna dipendenza da file locali del server: i test accedono ai `paths.*` **solo** se esposti pubblicamente.
