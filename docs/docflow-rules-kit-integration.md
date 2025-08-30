# Docflow Rules Kit Backend

## Technical Overview

### Architecture
- **DocflowRules.Sdk** provides runtime primitives for executing C# rule scripts. `ExtractionContext` stores field values and metadata, offering typed getters, setters and diff generation【F:stage/docflow-rules-kit/backend/src/DocflowRules.Sdk/Services/ExtractionContext.cs†L5-L52】.
- `RoslynScriptRunner` compiles and runs scripts, caching delegates and returning before/after snapshots with mutation logs【F:stage/docflow-rules-kit/backend/src/DocflowRules.Sdk/Services/Runner.cs†L44-L95】.
- **DocflowRules.Storage.EF** implements persistence with Entity Framework Core, exposing repository interfaces and an `AddDocflowRulesSqlite` extension to register the context and repositories with optional seeding of built‑in rules【F:stage/docflow-rules-kit/backend/src/DocflowRules.Storage.EF/Extensions/ServiceCollectionExtensions.cs†L6-L28】.
- Domain entities model rules (`RuleFunction`) and tests (`RuleTestCase`), including versioning and metadata fields【F:stage/docflow-rules-kit/backend/src/DocflowRules.Storage.EF/Domain/RuleFunction.cs†L3-L22】. `SeedData` initializes two built‑in rules for IBAN normalization and total calculation, each with sample tests【F:stage/docflow-rules-kit/backend/src/DocflowRules.Storage.EF/Seed/SeedData.cs†L14-L66】.
- **DocflowRules.Api** hosts REST controllers for managing rules, tests, LLM models and tags. It integrates local or remote language models through provider interfaces; `LlamaSharpProvider` loads GGUF models, tracks metrics, and generates test suggestions using a streaming chat session【F:stage/docflow-rules-kit/backend/src/DocflowRules.Api/LLM/LlamaSharpProvider.cs†L1-L119】.
- **DocflowRules.Worker** is a minimal service that exposes `/compile` and `/run` endpoints for script execution, secured by an API key middleware and instrumented with OpenTelemetry【F:stage/docflow-rules-kit/backend/src/DocflowRules.Worker/Program.cs†L1-L46】.

## Integration Plan for `src/DocflowAi.Net.Api`

1. **Project References**
   - Add `DocflowRules.Sdk` and `DocflowRules.Storage.EF` projects to the solution and reference them from `DocflowAi.Net.Api`.
   - Optionally include `DocflowRules.Worker` as a separate service or merge its functionality into the main API.

2. **Service Registration**
   - In `Program.cs`, register the rule engine and repositories:
     ```csharp
     builder.Services.AddDocflowRulesCore();
     builder.Services.AddDocflowRulesSqlite("Data Source=rules.db");
     ```
   - Adjust the connection string and seeding behavior to align with existing database settings.

3. **Database Migration**
   - Run EF Core migrations for the rules storage on startup or during deployment to ensure tables and built‑in rules are created.

4. **API Surface**
   - Map the controllers from `DocflowRules.Api` into the main application or port selected controllers (e.g., `RulesController`, `RuleTestsController`, `TagsController`) to expose rule management endpoints.
   - Ensure authentication and authorization policies match `DocflowAi.Net.Api` standards.
   - Convert `RulesController` into minimal API endpoints secured by API key for rule CRUD operations, staging/publishing, compilation and execution.
   - Convert `RuleTestsController` into minimal API endpoints to list, clone and run rule test cases with coverage summaries.
   - Convert `FuzzController` into minimal API endpoints to auto-generate boundary and missing-field test cases for a given rule.
   - Convert `PropertyController` into minimal API endpoints to run property-based checks on rules and import failing counterexamples.
   - Convert `TagsController` into minimal API endpoints to manage labels for rule test cases.
   - Convert `RuleBuilderController` into minimal API endpoints to validate and compile block-based rule definitions.
   - Convert `SuitesController` into minimal API endpoints to organize rule test cases into suites and support cloning.
   - Convert `LspProxyController` into minimal API endpoints that proxy WebSocket connections to a local C# language server and allow workspace file synchronization for IDE integrations.

5. **Worker Integration**
   - Deploy `DocflowRules.Worker` alongside the API or host its endpoints within the API project for local script compilation and execution.
   - Configure the main API to invoke `/compile` and `/run` when evaluating rules or running tests.

6. **LLM Configuration**
   - Choose a provider (`LlamaSharpProvider` for local GGUF models or `OpenAiProvider` for remote models) and supply model path, keys and runtime options through configuration.
   - Enable warm‑up on application start if large models require pre‑loading.

7. **Testing and Monitoring**
   - Integrate existing OpenTelemetry setup with the new services to trace rule execution and LLM calls.
   - Add unit and integration tests mirroring those in `stage/docflow-rules-kit/backend/tests` to verify rule execution, repository operations and API endpoints.

## Summary
The Docflow Rules Kit backend introduces a modular rule engine with script execution, persistent storage and LLM‑assisted testing. Integrating it into `DocflowAi.Net.Api` requires registering its services, migrating the database and exposing rule management and execution endpoints while reusing the worker and LLM provider infrastructure.
