# syntax=docker/dockerfile:1.7

########################################
# Global ARGs (single source of truth)
########################################
ARG LLM_DEFAULT_MODEL_REPO=unsloth/Qwen3-0.6B-GGUF
ARG LLM_DEFAULT_MODEL_FILE=Qwen3-0.6B-Q4_0.gguf
ARG LLM_MODEL_REV=main
ARG HF_TOKEN=""

#############################
# Frontend stage (Node 20)
#############################
FROM --platform=linux/amd64 node:20 AS frontend
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend ./
RUN npm run build

#############################
# Build stage (SDK 9.0)
#############################
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG API_PROJECT=src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj
WORKDIR /src

# Copy sources
COPY . .
# Copy frontend assets
COPY --from=frontend /src/frontend/dist ./src/DocflowAi.Net.Api/wwwroot

# Restore with explicit RID (linux-x64)
RUN dotnet restore "$API_PROJECT" -r linux-x64

# Publish: no single-file, no trim, target linux-x64
RUN dotnet publish "$API_PROJECT" -c Release -r linux-x64 \
    --self-contained false \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -o /app/publish

#############################
# Runtime stage (ASP.NET 9.0)
#############################
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-noble AS runtime

# Native deps + Python per Docling Serve (minimo indispensabile, NO Tesseract)
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      ca-certificates tini libgomp1 libstdc++6 libc6 libicu74 \
      python3 python3-pip libglib2.0-0 libgl1 libmagic1 \
  && mkdir -p /app/models \
  && update-ca-certificates \
  && rm -rf /var/lib/apt/lists/*

# Docling Serve (CPU) - default EasyOCR (no Tesseract)
RUN pip3 install --no-cache-dir docling-serve

# Re-import ARGs so they can be promoted to ENV
ARG LLM_DEFAULT_MODEL_REPO
ARG LLM_DEFAULT_MODEL_FILE
ARG LLM_MODEL_REV

# Common ENV (aggiungo solo DOCLING_PORT)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LLM__Provider="LLamaSharp" \
    DOCLING_PORT=5001 \
    LLAMASHARP__ContextSize=32768 \
    LLAMASHARP__Threads=0 \
    LLM__DefaultModelRepo=${LLM_DEFAULT_MODEL_REPO} \
    LLM__DefaultModelFile=${LLM_DEFAULT_MODEL_FILE} \
    LLM_MODEL_REV=${LLM_MODEL_REV} \
    OCR_DATA_PATH=/app/models

# Utente non-root e cartelle dati
RUN useradd -ms /bin/bash appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app

# Wrapper: avvia Docling Serve e poi la tua app .NET (senza heredoc)
RUN set -eux; \
  printf '%s\n' \
    '#!/usr/bin/env bash' \
    'set -euo pipefail' \
    '( docling-serve run --host 0.0.0.0 --port "${DOCLING_PORT:-5001}" ${DOCLING_SERVE_ENABLE_UI:+--enable-ui} ) &' \
    'exec /usr/local/bin/start.sh' \
    > /usr/local/bin/start-all.sh; \
  chmod +x /usr/local/bin/start-all.sh

USER appuser
WORKDIR /app

# App pubblicata
COPY --from=build --chown=appuser:appuser /app/publish ./

# Modelli (GGUF ecc.)
COPY --from=model --chown=appuser:appuser /models /home/appuser/models
ENV MODELS_DIR=/home/appuser/models

# Dove NuGet mette libllama.so
ENV LD_LIBRARY_PATH=/app/runtimes/linux-x64/native:$LD_LIBRARY_PATH

# Bootstrap originale
COPY --chown=appuser:appuser start.sh /usr/local/bin/start.sh
RUN chmod +x /usr/local/bin/start.sh

EXPOSE 8080
EXPOSE 5001
ENTRYPOINT ["/usr/bin/tini","--"]
CMD ["/usr/local/bin/start-all.sh"]
