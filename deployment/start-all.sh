#!/usr/bin/env bash
set -euo pipefail
python3 -m uvicorn py.main:app --host 0.0.0.0 --port 8000 &
exec dotnet api/DocflowAi.Net.Api.dll
