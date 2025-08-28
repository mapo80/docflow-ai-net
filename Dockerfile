# syntax=docker/dockerfile:1.7

########################################
# Global ARGs (single source of truth)
########################################
ARG LLM_DEFAULT_MODEL_REPO=unsloth/Qwen3-0.6B-GGUF
ARG LLM_DEFAULT_MODEL_FILE=Qwen3-0.6B-Q4_0.gguf
ARG LLM_MODEL_REV=main
ARG HF_TOKEN=""
# NEW: controlla installazione Docling a build-time (0=off default)
ARG DOCLING_INSTALL=0

#############################
# Frontend stage (Node 20)
#############################
FROM  node:20 AS frontend
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend ./
RUN npm run build

#############################
# Build stage (SDK 9.0)
#############################
FROM  mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
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
# Model stage (download GGUF)
#############################
FROM  mcr.microsoft.com/dotnet/aspnet:9.0-noble AS model
ARG LLM_DEFAULT_MODEL_REPO
ARG LLM_DEFAULT_MODEL_FILE
ARG LLM_MODEL_REV
ARG HF_TOKEN

RUN --mount=type=secret,id=hf_token,target=/run/secrets/hf_token \
    set -eu; \
    apt-get update && apt-get install -y --no-install-recommends ca-certificates curl && update-ca-certificates; \
    mkdir -p /models; \
    echo "[model] Downloading ${LLM_DEFAULT_MODEL_REPO}@${LLM_MODEL_REV}/${LLM_DEFAULT_MODEL_FILE}"; \
    TOKEN=""; \
    if [ -s /run/secrets/hf_token ]; then TOKEN="$(cat /run/secrets/hf_token || true)"; \
    elif [ -n "$HF_TOKEN" ]; then TOKEN="$HF_TOKEN"; fi; \
    URL="https://huggingface.co/${LLM_DEFAULT_MODEL_REPO}/resolve/${LLM_MODEL_REV}/${LLM_DEFAULT_MODEL_FILE}?download=true"; \
    if [ -n "$TOKEN" ]; then \
      curl -fSL -H "Authorization: Bearer ${TOKEN}" -o "/models/${LLM_DEFAULT_MODEL_FILE}.part" "$URL"; \
    else \
      echo "[model] No token provided: public download"; \
      curl -fSL -o "/models/${LLM_DEFAULT_MODEL_FILE}.part" "$URL"; \
    fi; \
    mv "/models/${LLM_DEFAULT_MODEL_FILE}.part" "/models/${LLM_DEFAULT_MODEL_FILE}"; \
    ls -lh /models; \
    apt-get purge -y curl && rm -rf /var/lib/apt/lists/*

#############################
# Runtime stage (ASP.NET 9.0)
#############################
FROM  mcr.microsoft.com/dotnet/aspnet:9.0-noble AS runtime

# Bring build-time flag into scope
ARG DOCLING_INSTALL

# Base deps + OpenSSH per App Service (SSH su 2222)
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      ca-certificates tini libgomp1 libstdc++6 libc6 libicu74 \
      libglib2.0-0 libmagic1 openssh-server curl \
  && mkdir -p /app/models /var/run/sshd \
  && update-ca-certificates \
  && rm -rf /var/lib/apt/lists/*

# Configura SSHD su 2222 (App Service) e abilita root login con password (solo per WebSSH)
RUN sed -i 's/^#\?Port .*/Port 2222/' /etc/ssh/sshd_config \
 && sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication yes/' /etc/ssh/sshd_config \
 && sed -i 's/^#\?PermitRootLogin .*/PermitRootLogin yes/' /etc/ssh/sshd_config \
 && echo 'root:Docker!' | chpasswd \
 && ssh-keygen -A

# --- DOCLING (facoltativo) ---
# Installa Python/venv + docling-serve SOLO se DOCLING_INSTALL=1
RUN if [ "${DOCLING_INSTALL}" = "1" ]; then \
      set -eux; \
      apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        python3 python3-pip python3-venv && \
      python3 -m venv /opt/venv && \
      /opt/venv/bin/python -m pip install --no-cache-dir --upgrade pip && \
      /opt/venv/bin/pip install --no-cache-dir \
        --index-url https://download.pytorch.org/whl/cpu \
        torch torchvision && \
      /opt/venv/bin/pip install --no-cache-dir opencv-python-headless docling-serve && \
      rm -rf /var/lib/apt/lists/*; \
    fi

# Re-import ARGs so they can be promoted to ENV
ARG LLM_DEFAULT_MODEL_REPO
ARG LLM_DEFAULT_MODEL_FILE
ARG LLM_MODEL_REV

# Common ENV
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LLM__Provider="LLamaSharp" \
    LLAMASHARP__ContextSize=32768 \
    LLAMASHARP__Threads=0 \
    LLM__DefaultModelRepo=${LLM_DEFAULT_MODEL_REPO} \
    LLM__DefaultModelFile=${LLM_DEFAULT_MODEL_FILE} \
    LLM_MODEL_REV=${LLM_MODEL_REV} \
    OCR_DATA_PATH=/app/models \
    DOCLING_PORT=5001 \
    DOCLING_ENABLED=0

# Utente non-root e cartelle dati
RUN useradd -ms /bin/bash appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app

# Bootstrap originale (avvia l'app .NET)
COPY --chown=appuser:appuser start.sh /usr/local/bin/start.sh
RUN chmod +x /usr/local/bin/start.sh

# Wrapper: avvia Docling-Serve SOLO se DOCLING_ENABLED=1, poi la tua app .NET
RUN set -eux; \
  printf '%s\n' \
    '#!/usr/bin/env bash' \
    'set -euo pipefail' \
    '' \
    'export PYTHONUNBUFFERED=1' \
    '' \
    'if [ "${DOCLING_ENABLED:-0}" = "1" ]; then' \
    '  if [ ! -x /opt/venv/bin/docling-serve ]; then' \
    '    echo "[startup] Docling abilitato ma non installato: ricostruisci con --build-arg DOCLING_INSTALL=1";' \
    '    exit 1' \
    '  fi' \
    '  ( /opt/venv/bin/docling-serve run --host 0.0.0.0 --port "${DOCLING_PORT:-5001}" ${DOCLING_SERVE_ENABLE_UI:+--enable-ui} ) & ' \
    '  for i in $(seq 1 120); do' \
    '    if curl -fsS "http://127.0.0.1:${DOCLING_PORT:-5001}/docs" >/dev/null; then' \
    '      echo "[startup] Docling-Serve è UP su :${DOCLING_PORT:-5001}"; break; fi' \
    '    echo "[startup] Attendo Docling-Serve... ($i/120)"; sleep 1;' \
    '  done' \
    '  curl -fsS "http://127.0.0.1:${DOCLING_PORT:-5001}/docs" >/dev/null || { echo "[startup] ERRORE: Docling non è partito"; exit 1; }' \
    'else' \
    '  echo "[startup] Docling disabilitato (DOCLING_ENABLED=0)";' \
    'fi' \
    '' \
    'exec /usr/local/bin/start.sh' \
    > /usr/local/bin/start-all.sh; \
  chmod +x /usr/local/bin/start-all.sh

WORKDIR /app

# App pubblicata
COPY --from=build --chown=appuser:appuser /app/publish ./

# Modelli (GGUF ecc.)
COPY --from=model --chown=appuser:appuser /models /home/appuser/models
ENV MODELS_DIR=/home/appuser/models

# Dove NuGet mette libllama.so
ENV LD_LIBRARY_PATH=/app/runtimes/linux-x64/native:$LD_LIBRARY_PATH

EXPOSE 8080
EXPOSE 5001
EXPOSE 2222

ENTRYPOINT ["/usr/bin/tini","--"]
CMD ["/usr/local/bin/start-all.sh"]
