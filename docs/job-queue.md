# Job Queue

The API uses Hangfire with in-memory storage for scheduling and LiteDB as the single source of truth for job state. Files on disk are only artifacts and never used to derive status.

## States
Submit → `Queued` → `Running` → `Succeeded`/`Failed`/`Cancelled`

Derived status is computed from the database status only:

| Status | derivedStatus |
|--------|---------------|
| Queued | Pending       |
| Running| Processing    |
| Succeeded | Completed  |
| Failed | Failed       |
| Cancelled | Cancelled |

## Job directory layout
`{DataRoot}/{jobId}/`
- `input.*`
- `prompt.txt` or `.json`
- `fields.json`
- `manifest.json`
- `output.json` (on success)
- `error.txt` (on error)

## Configuration
See `appsettings.json` section `JobQueue` for paths, rate limits and cleanup schedule. Dashboard auth can be enabled with `HangfireDashboardAuth`.

### Immediate mode
Jobs can be executed inline by calling `POST /api/v1/jobs?mode=immediate` when `JobQueue.Immediate.Enabled=true`.
The mode consumes the same concurrency gate as queued jobs and respects global backpressure, idempotency and dedupe rules.
If capacity is unavailable the API returns `429 { error:"immediate_capacity" }` unless `FallbackToQueue` is true, in which case the job is enqueued and `202` is returned.
Timeout for immediate execution is controlled by `JobQueue.Immediate.TimeoutSeconds` and should be lower than the ingress timeout.
The old `/api/v1/process` endpoint has been removed; all processing now flows through the job queue.

## Examples
```
# submit
curl -X POST /api/v1/jobs -H "Content-Type: application/json" -d '{"fileBase64":"...","fileName":"a.pdf"}'
# list
curl /api/v1/jobs?page=1&pageSize=20
# get by id
curl /api/v1/jobs/{id}
# cancel
curl -X DELETE /api/v1/jobs/{id}
```

The dashboard is exposed at `/hangfire` when enabled. Protect it with basic auth credentials via configuration. Only non-executable files up to the configured size are accepted. Logs omit sensitive data and include structured properties for observability.
