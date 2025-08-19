# Job Queue

The API uses Hangfire with in-memory storage for scheduling and a relational database accessed through Entity Framework Core (SQLite by default) as the single source of truth for job state. Files on disk are only artifacts and never used to derive status.

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
See `appsettings.json` section `JobQueue` for paths, rate limits and cleanup schedule. Setting `JobQueue.EnableHangfireDashboard` exposes the dashboard and requires the same API keys as the REST API.

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

The dashboard is exposed at `/hangfire` when enabled. Protect it with an API key via configuration and supply it with the `api_key` query parameter or `X-API-Key` header. Only non-executable files up to the configured size are accepted. Logs omit sensitive data and include structured properties for observability.
