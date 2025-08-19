#!/usr/bin/env bash
set -euo pipefail

MODEL_FILE="${LLM__DefaultModelFile:-Qwen3-1.7B-UD-Q4_K_XL.gguf}"
MODEL_DIR="/home/appuser/models"
TARGET="${MODEL_DIR}/${MODEL_FILE}"

echo "[start] using model: ${TARGET}"

# The model MUST already exist (downloaded during build)
if [ ! -f "${TARGET}" ]; then
  echo "[start][ERROR] Model not found: ${TARGET}"
  echo " - Rebuild the image ensuring the model is downloaded in the 'model' stage"
  echo " - Or mount the volume with the GGUF file in ${MODEL_DIR}"
  exit 1
fi

# Export models directory
export MODELS_DIR="${MODEL_DIR}"

# LLamaSharp threads: if 0 use nproc
if [ "${LLAMASHARP__Threads:-0}" = "0" ]; then
  export LLAMASHARP__Threads="$(nproc)"
fi

echo "[start] starting API on :8080 (LLamaSharp in-process)"
exec dotnet DocflowAi.Net.Api.dll
