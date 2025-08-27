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
# Model stage (download GGUF)
#############################
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-noble AS model
# Bring global ARGs into scope (no values = no duplication)
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
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-noble AS runtime

# Native dependencies for libllama on Ubuntu 24.04 (Noble)
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      ca-certificates tini libgomp1 libstdc++6 libc6 libicu74 \
  && mkdir -p /app/models \
  && update-ca-certificates \
  && rm -rf /var/lib/apt/lists/*


# Re-import ARGs so they can be promoted to ENV
ARG LLM_DEFAULT_MODEL_REPO
ARG LLM_DEFAULT_MODEL_FILE
ARG LLM_MODEL_REV

# Common ENV (derived from ARGs) — no default duplication
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LLM__Provider="LLamaSharp" \
    LLAMASHARP__ContextSize=32768 \
    LLAMASHARP__Threads=0 \
    LLM__DefaultModelRepo=${LLM_DEFAULT_MODEL_REPO} \
    LLM__DefaultModelFile=${LLM_DEFAULT_MODEL_FILE} \
    LLM_MODEL_REV=${LLM_MODEL_REV} \
    OCR_DATA_PATH=/app/models

# Non-root user
RUN useradd -ms /bin/bash appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app
USER appuser
WORKDIR /app

# Published app
COPY --from=build --chown=appuser:appuser /app/publish ./

# Models
COPY --from=model --chown=appuser:appuser /models /home/appuser/models

# directory where models are stored
ENV MODELS_DIR=/home/appuser/models

# Where NuGet places libllama.so
ENV LD_LIBRARY_PATH=/app/runtimes/linux-x64/native:$LD_LIBRARY_PATH

# Bootstrap
COPY --chown=appuser:appuser start.sh /usr/local/bin/start.sh
RUN chmod +x /usr/local/bin/start.sh

EXPOSE 8080
ENTRYPOINT ["/usr/bin/tini","--"]
CMD ["/usr/local/bin/start.sh"]
