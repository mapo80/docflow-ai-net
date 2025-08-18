# Models Management Refactor

## Overview
Rebuild the Models management experience with a fullscreen create/edit modal and client-side state updates that avoid page reloads. The modal must preserve its state during all asynchronous operations.

## Model Types
1. **Hosted LLM** (OpenAI or Azure OpenAI)
   - Fields: `provider` (`openai`|`azure-openai`), `baseUrl`, `apiKey`.
   - Action: *Test Connection*.
2. **Local Hugging Face**
   - Fields: `hfToken`, `hfRepo`, `modelFile`.
   - Option: `downloadImmediately` checkbox.
   - Actions: *Start Download*, *Cancel Download*, *Show Status/Progress*.

## UI Requirements
- Models list columns: **Name**, **Type**, **Provider / HF Repo+File**, **Downloaded** (`Yes`, `No`, `–` for hosted), **Download Status**, **Actions**.
- Fullscreen modal for create/edit with type switch; display only fields relevant to the chosen type.
- Client state must update without window refresh.
- Download progress shown live (polling or SSE every 1–3 s) with percentage, bytes and speed.
- Minimum download states: `Pending`, `Downloading`, `Downloaded`, `Failed`, `Canceled`.
- Secrets are masked in the UI and never returned by APIs after save.

## API Contracts
- `GET /api/models`
- `GET /api/models/{id}` (no secrets)
- `POST /api/models` (`type=local` and `downloadNow=true` enqueues download)
- `PATCH /api/models/{id}` (allows secret rotation)
- `DELETE /api/models/{id}` (option `deleteLocalFile`)
- `POST /api/models/{id}/download` (local only)
- `POST /api/models/{id}/cancel-download` (local only)
- `GET /api/models/{id}/download-status`
- `POST /api/models/test-connection` (hosted only)

## Database Summary
`Models` table columns:
- Common: `Id`, `Name` (unique), `Type` (`hosted-llm`|`local`), `IsActive`, `LastUsedAt`, `CreatedAt`, `UpdatedAt`.
- Hosted: `Provider`, `BaseUrl`, `ApiKeyEncrypted`.
- Local: `HfRepo`, `ModelFile`, `HfTokenEncrypted`, `DownloadStatus`, `Downloaded` (nullable for hosted), `DownloadedAt`, `LocalPath`, `FileSizeBytes`, `Checksum`.
- Insert records immediately on create and track download status for local models.

## Hugging Face Download
- Background job supports resume, retry and cancel.
- Uses temporary file with atomic rename on completion.
- Periodically updates status (bytes, percent, speed).
- On success set `Downloaded=true` and `DownloadedAt`.

## Security
- Encrypt secrets at rest.
- APIs never return secrets.
- UI shows `•••••` placeholders for sensitive fields.

## Tests
- **Backend** unit/integration: hosted and local creation, `downloadNow`, download status transitions, idempotent start, `test-connection` success/failure.
- **Frontend** E2E: create and edit both model types, live download progress within modal, list actions, and no page refresh.

## Seeding
Seed a default local model already downloaded:
- `HfRepo = unsloth/Qwen3-0.6B-GGUF`
- `ModelFile = Qwen3-0.6B-Q4_0.gguf`

