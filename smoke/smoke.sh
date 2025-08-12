#!/usr/bin/env bash
set -euo pipefail
API_KEY="${API_KEY:-dev-secret-key-change-me}"
REASONING="${REASONING:-no_think}"
API_URL="${API_URL:-http://localhost:5214}"
MD_URL="${MD_URL:-http://localhost:8000}"
MODEL_PATH="${MODEL_PATH:-../models/qwen3-1_5-instruct-q4_0.gguf}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
proj_root="$(cd "${here}/.." && pwd)"
pretty() { python3 - <<'PY' || cat
import sys, json
try: print(json.dumps(json.load(sys.stdin), indent=2, ensure_ascii=False))
except Exception: sys.exit(1)
PY
}
cd "${proj_root}/deployment"
docker compose up -d --build
for i in {1..30}; do curl -sf "${MD_URL}/health" >/dev/null && break || sleep 1; done
for i in {1..60}; do curl -sf "${API_URL}/health" >/dev/null && break || sleep 1; done
curl -s -F "file=@${here}/sample.png" "${MD_URL}/markdown" | pretty | tee "${here}/markitdown.json" >/dev/null
if [ -f "${proj_root}/models/$(basename "${MODEL_PATH}")" ]; then
  curl -s -X POST "${API_URL}/api/v1/process" -H "X-API-Key: ${API_KEY}" -H "X-Reasoning: ${REASONING}" -F "file=@${here}/sample.png" | pretty | tee "${here}/process.json" >/dev/null
else
  echo "NOTE: Model not found in ./models — skipping /process E2E"
fi
