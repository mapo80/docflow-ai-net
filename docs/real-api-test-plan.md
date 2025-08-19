# INTEGRATION TEST PLAN (REAL API) — BACKEND (C#)

Objective: run real end-to-end tests against the exposed APIs (no mocks) using real files and services. Tests do not modify the backend and verify observable behavior via REST.
Endpoint scope:
- POST /api/v1/jobs
- GET /api/v1/jobs (paged list)
- GET /api/v1/jobs/{id}
- DELETE /api/v1/jobs/{id}
- GET /health/live
- GET /health/ready
- (Hangfire dashboard: only check reachability via public URL, no API)

⚙️ **ON-DEMAND EXECUTION**
- Framework: **xUnit** (or NUnit), HTTP library: **HttpClient**. No stubs/mocks.
- Test category: `Trait("Category","RealE2E")` (filterable with `--filter`).
- Enabled via env vars (example): `DOCFLOW_API_BASE_URL=https://<host>` and optional `DOCFLOW_HANGFIRE_PATH=/hangfire`.
- Tests automatically skip if the base URL is not set.

📦 **DATA & PREREQUISITES**
- Test files (folder `TestData/` in the test project):
  1) `sample.pdf` ~100–200KB (valid PDF)
  2) `sample-large.pdf` > `UploadLimits.MaxRequestBodyMB` (e.g. 20% over)
  3) `image.png` ~100KB (allowed MIME)
  4) `evil.exe` ~10KB (forbidden MIME/extension)
- Sample payloads:
  - `prompt_text`: "Estrai intestatario, totale, iva."
  - `prompt_json`: `{ "task": "extract", "entities": ["buyer","total","vat"] }`
  - `fields_json`: `{ "entities": [{"name":"buyer","type":"string"},{"name":"total","type":"number"},{"name":"vat","type":"number"}] }`
- Test utilities:
  - `Idempotency-Key` generator (GUID and SHA-256 of file+prompt+fields).
  - Read both `Retry-After` header and `retry_after_seconds` body.

🧪 **SUITE & CASES (Arrange → Act → Assert)**

### A) SMOKE & HEALTH
A1. **Live_OK** – GET /health/live → 200.

A2. **Ready_OK_or_Reasons** – GET /health/ready → 200 **or** 503 with `reasons[]` among: `db_unavailable`, `data_root_not_writable`, `backpressure`. If 503: assert `reasons` is not empty.

A3. **Hangfire_Accessible** (optional) – If `DOCFLOW_HANGFIRE_PATH` is set, perform a HEAD/GET to the public URL; expect 200/401/403/redirect (must not be persistent 404/5xx).

### B) SUBMIT — QUEUED (202)
B1. **Submit_Queued_202_Minimal** – POST /api/v1/jobs with `sample.pdf`, `prompt_text`, `fields_json` → 202 `{ job_id, status_url }`. Verify GET {id} returns status `Queued` or `Running`.

B2. **Submit_Queued_202_IdempotencyKey** – Repeat the same POST with header `Idempotency-Key: <K>` → 202 with the **same** `job_id`.

B3. **Submit_Queued_202_DedupeHash** – Two sequential POSTs **without** Idempotency-Key but with the **same** file/prompt/fields → 202 with the **same** `job_id` (dedupe window).

### D) PAGED LIST — GET /api/v1/jobs
D1. **List_Default_Paged_Desc** – GET /api/v1/jobs?page=1&pageSize=20 → 200 { page, pageSize, total, items }. Verify `items` sorted by `createdAt` DESC (compare `createdAt` of first 3).

D2. **List_Pagination_NextPage** – If `total` > pageSize, GET page=2 → different items and total consistency.

D3. **List_Filters_ClientSide** (FE only) – Backend exposes no dedicated filters; validate locally that FE-filtered subset matches expected `status` values.

### E) DETAIL — GET /api/v1/jobs/{id}
E1. **Get_ById_Existing** – From job created in B1: GET {id} → 200; assert basic fields (status, derivedStatus, progress, timestamps). If paths present: do **not** infer state from files, only verify presence of paths.

E2. **Get_ById_NotFound** – GET random guid → 404 `{ error:"not_found" }`.

### F) CANCEL — DELETE /api/v1/jobs/{id}
F1. **Cancel_Queued_202** – On a `Queued` job: DELETE → 202, GET {id} → `Cancelled`.

F2. **Cancel_Running_202** (if possible) – On a `Running` job: DELETE → 202, GET {id} → `Cancelled` (or briefly `Running`, then `Cancelled`).

F3. **Cancel_Terminal_409** – On `Succeeded` or `Failed` job: DELETE → 409 `{ error:"conflict" }`.

### G) BACKPRESSURE & RATE LIMIT (429)
G1. **QueueFull_429_SubmitQueued** – Precondition: queue full (perform N submits until GET /health/ready reports reason `backpressure` or until a 429 is returned). New queued POST → 429 `{ error:"queue_full" }` + `Retry-After` header/body.

G3. **RateLimited_429** (if policy active) – Send >N submits in a short window → 429 `{ error:"rate_limited" }` + `Retry-After`.

### H) PAYLOAD & VALIDATION ERRORS
H1. **PayloadTooLarge_413** – Queued POST with `sample-large.pdf` → 413 `{ error:"payload_too_large" }`.

H2. **MimeNotAllowed_400** – POST with `evil.exe` (octet-stream) → 400 `{ error:"bad_request" }`.

H3. **BadJson_400** – Invalid JSON for `fields` or `prompt` → 400.

H4. **InsufficientStorage_507** (if reproducible) – POST triggering 507 → 507 `{ error:"insufficient_storage" }`.

### I) CONSISTENCY, IDP & DEDUPE
I1. **IdempotencyKey_Reused_AfterRestart** (if restart possible) – Submit with same `Idempotency-Key` **before and after** service restart → same `job_id`.

I2. **HashDedupe_Window** – Two identical submits within dedupe TTL → same `job_id`; after TTL → different job.

### L) ARTIFACTS (paths.*)
L1. **OutputJson_Available_OnSuccess** (if exposed) – For `Succeeded` job: HTTP GET to `paths.output` (if public) → 200 and `application/json`.

L2. **ErrorTxt_Available_OnFailure** (if exposed) – For `Failed` job: HTTP GET `paths.error` → 200 and `text/plain`.

🧼 **CLEAN-UP**
- Terminal jobs remain in system (nightly TTL cleanup); no "hard delete" via API.
- Tests generate prefixes in prompts (e.g., `"[ITEST-<RunId>] ..."`) to aid maintenance.
- `Queued/Running` jobs created by tests must be cancelled with DELETE (F1/F2).

📊 **OUTPUT & LOGGING**
- Each test logs: `RequestId` (if header), `JobId`, timings, outcome, any `Retry-After`.
- Produce a **JUnit/TRX** report and a CSV with: TestId, JobId, FinalState, Duration, Error.

▶️ **EXECUTION (example)**
- Preconditions: backend reachable with real services.
- Commands:
  - `set DOCFLOW_API_BASE_URL=https://host:port` (or export on Linux/Mac)
  - `dotnet test -c Release --filter Category=RealE2E`
  - (optional) `--logger "trx;LogFileName=real-e2e.trx"`

✅ **ACCEPTANCE CRITERIA**
- All cases A–H pass. Optional cases (C4, F2, G3, H4, I1) pass if the environment allows reproduction.
- For 429 responses, `Retry-After` is present in header or body.
- Health: `/health/live` 200; `/health/ready` → 200 or 503 with non-empty reasons.
- No dependency on server-local files: tests access `paths.*` only if publicly exposed.
