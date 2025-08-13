# syntax=docker/dockerfile:1.7

# ============================
# Build stage (SDK 9.0 - Noble)
# ============================
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG API_PROJECT=src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj
ENV DEBIAN_FRONTEND=noninteractive
WORKDIR /src
COPY . .
RUN dotnet restore "$API_PROJECT" \
 && dotnet publish "$API_PROJECT" -c Release -o /app/publish --no-restore

# ============================
# Model stage (scarica GGUF a build-time)
# ============================
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-noble AS model

ARG LLM_MODEL_REPO=unsloth/Qwen3-1.7B-GGUF
ARG LLM_MODEL_FILE=Qwen3-1.7B-UD-Q4_K_XL.gguf
ARG LLM_MODEL_REV=main
# Fallback opzionale via --build-arg
ARG HF_TOKEN=""

RUN --mount=type=secret,id=hf_token,target=/run/secrets/hf_token \
    set -eu; \
    apt-get update && apt-get install -y --no-install-recommends ca-certificates curl; \
    mkdir -p /models; \
    echo "[model] Scarico ${LLM_MODEL_REPO}@${LLM_MODEL_REV}/${LLM_MODEL_FILE}"; \
    TOKEN=""; \
    if [ -s /run/secrets/hf_token ]; then \
      TOKEN="$(cat /run/secrets/hf_token || true)"; \
    elif [ -n "$HF_TOKEN" ]; then \
      TOKEN="$HF_TOKEN"; \
    fi; \
    URL="https://huggingface.co/${LLM_MODEL_REPO}/resolve/${LLM_MODEL_REV}/${LLM_MODEL_FILE}?download=true"; \
    if [ -n "$TOKEN" ]; then \
      curl -fSL -H "Authorization: Bearer ${TOKEN}" -o "/models/${LLM_MODEL_FILE}.part" "$URL"; \
    else \
      echo "[model] Nessun token fornito: provo download senza auth (repo pubblico richiesto)"; \
      curl -fSL -o "/models/${LLM_MODEL_FILE}.part" "$URL"; \
    fi; \
    mv "/models/${LLM_MODEL_FILE}.part" "/models/${LLM_MODEL_FILE}"; \
    ls -lh /models; \
    apt-get purge -y curl && rm -rf /var/lib/apt/lists/*

# ============================
# Runtime stage (ASP.NET 9.0 - Noble)
# ============================
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-noble AS runtime

ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LLM_MODEL_REPO="unsloth/Qwen3-1.7B-GGUF" \
    LLM_MODEL_FILE="Qwen3-1.7B-UD-Q4_K_XL.gguf" \
    LLM_MODEL_REV="main" \
    LLAMASHARP__ContextSize=8192 \
    LLAMASHARP__Threads=0 \
    LLM__Provider="LLamaSharp"
# NOTA: niente token e niente path hardcoded qui; lo imposta start.sh

RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates tini \
 && rm -rf /var/lib/apt/lists/*

RUN useradd -ms /bin/bash appuser
USER appuser
WORKDIR /app

# App e modello (già scaricato)
COPY --from=build /app/publish ./
COPY --from=model --chown=appuser:appuser /models /home/appuser/models

# Bootstrap minimale
COPY --chown=appuser:appuser start.sh /usr/local/bin/start.sh
RUN chmod +x /usr/local/bin/start.sh

EXPOSE 8080
ENTRYPOINT ["/usr/bin/tini","--"]
CMD ["/usr/local/bin/start.sh"]
