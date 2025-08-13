#!/usr/bin/env bash
set -euo pipefail

MODEL_FILE="${LLM_MODEL_FILE:-Qwen3-1.7B-UD-Q4_K_XL.gguf}"
MODEL_DIR="/home/appuser/models"
TARGET="${LLM__ModelPath:-${MODEL_DIR}/${MODEL_FILE}}"

echo "[start] uso modello: ${TARGET}"

# Il modello DEVE essere già presente (copiato in build)
if [ ! -f "${TARGET}" ]; then
  echo "[start][ERROR] Modello non trovato: ${TARGET}"
  echo " - Ricompila l'immagine assicurandoti di scaricare il modello nello stage 'model'"
  echo " - Oppure monta il volume con il file GGUF in ${MODEL_DIR} e imposta LLM__ModelPath"
  exit 1
fi

# Assicura che LLM__ModelPath punti al file effettivo
export LLM__ModelPath="${TARGET}"

# Threads LLamaSharp: se 0 → nproc
if [ "${LLAMASHARP__Threads:-0}" = "0" ]; then
  export LLAMASHARP__Threads="$(nproc)"
fi

echo "[start] avvio API su :8080 (LLamaSharp in-process)"
exec dotnet DocflowAi.Net.Api.dll
