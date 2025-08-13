# Agents (Codex) Guide

- Call `POST /api/v1/process` with headers:
  - `X-API-Key`
  - optional `X-Reasoning: think|no_think|auto`
- Ensure a GGUF model is mounted at `/models` (compose does it).
- For local tests download `qwen2.5-0.5b-instruct-q4_0.gguf` into `./models`:
  ```bash
  export HF_TOKEN="<il tuo token>"
    huggingface-cli download Qwen/Qwen2.5-0.5B-Instruct-GGUF \
      qwen2.5-0.5b-instruct-q4_0.gguf \
      --local-dir ./models --token "$HF_TOKEN"
  export LLM__ModelPath="$(pwd)/models/qwen2.5-0.5b-instruct-q4_0.gguf"
  ```
- Output is always **valid JSON** due to **GBNF grammar** at inference, then validated against **Extraction Profiles**.

Prompts:
- The server injects `/think` or `/no_think` automatically based on header/config.
- Do not add explanations; responses must be pure JSON.
## Workflow

- Initialize submodules:
  ```bash
  git submodule update --init --recursive
  ```
- Install the .NET 9 SDK locally if needed:
  ```bash
  ./dotnet-install.sh --version 9.0.100 --install-dir "$HOME/dotnet"
  export PATH="$HOME/dotnet:$PATH"
  ```
- Build and test:
  ```bash
  dotnet build -c Release
  dotnet test -c Release
  ```
- Dockerize:
  ```bash
  docker build -f deployment/Dockerfile.api -t docflow-api .
  docker run --rm -p 8080:8080 docflow-api
  ```
