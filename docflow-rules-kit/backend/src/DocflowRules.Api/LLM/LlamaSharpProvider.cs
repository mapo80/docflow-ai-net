using System.Text;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using DocflowRules.Api.Services;
using Microsoft.Extensions.Configuration;
using LLama;
using LLama.Common;

namespace DocflowRules.Api.LLM;

public class LlamaSharpProvider : ILLMProvider, DocflowRules.Api.Services.ILLMProviderConfigurable, IDisposable
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<LlamaSharpProvider> _log;
    private LLamaWeights? _weights;
    private DocflowRules.Storage.EF.LlmModel? _runtimeModel;
    private Guid? _loadedModelId;
    private bool _needsReload;
    private LLamaContext? _context;
    private ChatSession? _session;
    private readonly object _lock = new();

    static readonly ActivitySource Activity = new("DocflowRules.LLM");
    static readonly Meter Meter = new("DocflowRules.LLM");
    static readonly Counter<long> Calls = Meter.CreateCounter<long>("llm.calls");
    static readonly Counter<long> Errors = Meter.CreateCounter<long>("llm.errors");
    static readonly Histogram<double> LatencyMs = Meter.CreateHistogram<double>("llm.latency.ms");

    public LlamaSharpProvider(IConfiguration cfg, ILogger<LlamaSharpProvider> log)
    {
        _cfg = cfg; _log = log;
    }

    public void SetRuntimeConfig(DocflowRules.Storage.EF.LlmModel model, bool turbo)
    {
        lock(_lock) { _runtimeModel = model; _needsReload = _loadedModelId != model.Id; }
    }

    public Task WarmupAsync(CancellationToken ct)
    {
        EnsureModel(); if (_needsReload) { try { _session?.Dispose(); _context?.Dispose(); _weights?.Dispose(); } catch {} _session=null; _context=null; _weights=null; EnsureModel(); } return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _session?.Dispose(); } catch {}
        try { _context?.Dispose(); } catch {}
        try { _weights?.Dispose(); } catch {}
    }

    private void EnsureModel()
    {
        if (_session != null) return;
        lock (_lock)
        {
            if (_session != null) return;
            var path = _runtimeModel?.ModelPathOrId ?? _cfg["LLM:Local:ModelPath"];
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                throw new InvalidOperationException("LLM:Local:ModelPath non configurato o file mancante");

            int nCtx = _runtimeModel?.ContextSize ?? (int.TryParse(_cfg["LLM:Local:ContextSize"], out var c) ? c : 4096);
            int threads = _runtimeModel?.Threads ?? (int.TryParse(_cfg["LLM:Local:Threads"], out var t) ? t : Math.Max(1, Environment.ProcessorCount - 1));
            int batch = _runtimeModel?.BatchSize ?? (int.TryParse(_cfg["LLM:Local:BatchSize"], out var b) ? b : 2048);
            int seed = int.TryParse(_cfg["LLM:Local:Seed"], out var s) ? s : 1337;

            _log.LogInformation("Loading GGUF model from {Path} (ctx={Ctx}, threads={Threads}, batch={Batch})", path, nCtx, threads, batch);
            _weights = LLamaWeights.LoadFromFile(new ModelParams(path) { ContextSize = nCtx, GpuLayerCount = 0 });
            _context = _weights.CreateContext(new ContextParams { ContextSize = nCtx, Seed = seed, Threads = threads });
            var ex = new InteractiveExecutor(_context);
            _session = new ChatSession(ex);
            _loadedModelId = _runtimeModel?.Id; _needsReload = false;
        }
    }

    public async Task<(JsonArray Refined, string Model, int InputTokens, int OutputTokens, long DurationMs, double CostUsd)> RefineAsync(string ruleName, string code, JsonArray skeletons, string? userPrompt, int budget, double temperature, CancellationToken ct)
    {
        using var act = Activity.StartActivity("LLM.Refine.Local", ActivityKind.Internal);
        Calls.Add(1);
        var start = DateTimeOffset.UtcNow;
        try
        {
            EnsureModel(); if (_needsReload) { try { _session?.Dispose(); _context?.Dispose(); _weights?.Dispose(); } catch {} _session=null; _context=null; _weights=null; EnsureModel(); }
            var modelName = System.IO.Path.GetFileName(_cfg["LLM:Local:ModelPath"] ?? "local.gguf");

            var sb = new StringBuilder();
            sb.AppendLine("Sei un assistente che genera casi di test per funzioni di validazione/normalizzazione documentale.");
            sb.AppendLine("RISPONDI SOLO CON UN JSON: un array di oggetti con campi {name, input, expect:{fields:{}}, suite, tags, priority}.");
            sb.AppendLine("Usa equals/approx/regex/exists in modo appropriato. Evita testo extra, non usare commenti.");
            sb.AppendLine();
            sb.AppendLine("=== Contesto ===");
            sb.AppendLine($"Rule: {ruleName}");
            var trimmed = code.Length > 12000 ? code.Substring(0,12000) : code;
            sb.AppendLine("Code:
"""
" + trimmed + "
"""");
            sb.AppendLine();
            sb.AppendLine("Skeletons (JSON):");
            sb.AppendLine(skeletons.ToJsonString());
            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                sb.AppendLine();
                sb.AppendLine("Istruzioni aggiuntive dell'utente:");
                sb.AppendLine(userPrompt);
            }
            sb.AppendLine();
            sb.AppendLine($"Genera fino a {budget} test. Rispondi con UN JSON valido (array).");

            var prompt = sb.ToString();

            // Generate
            var maxTok = _runtimeModel?.MaxTokens ?? (int.TryParse(_cfg["LLM:Local:MaxTokens"], out var mt) ? mt : 2048);
            var temp = _runtimeModel?.Temperature ?? (float)(temperature <= 0 ? 0.1 : temperature);
            if (temperature < 0.05) temp = 0.05f;
            var inferParams = new InferenceParams
            {
                MaxTokens = maxTok,
                Temperature = (float)temp,
                AntiPrompts = new List<string> { "</s>", "```" }
            };

            var builder = new StringBuilder();
            await foreach (var token in _session!.ChatAsync(new ChatHistory()
                .AddMessage(AuthorRole.System, "Devi rispondere solo con JSON valido. Niente testo extra.")
                .AddMessage(AuthorRole.User, prompt), inferParams, ct))
            {
                builder.Append(token);
                if (builder.Length > 64*1024) break; // guardrail
            }
            var output = builder.ToString();

            // Extract JSON array or suggestions property
            JsonArray arr;
            try
            {
                var node = System.Text.Json.JsonNode.Parse(output);
                if (node is JsonArray ja) arr = ja;
                else if (node is JsonObject jo && jo["suggestions"] is JsonArray ja2) arr = ja2;
                else
                {
                    // Try to extract first JSON array with a naive bracket matching
                    var first = ExtractFirstJsonArray(output);
                    arr = first ?? new JsonArray();
                }
            }
            catch
            {
                var first = ExtractFirstJsonArray(output);
                arr = first ?? new JsonArray();
            }

            var dur = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            LatencyMs.Record(dur);
            act?.SetTag("llm.local.model", modelName);
            act?.SetTag("llm.duration_ms", dur);
            act?.SetStatus(ActivityStatusCode.Ok);

            // Token counts & cost non disponibili con certezza: restituiamo 0; costo 0 (on-prem)
            return (arr, modelName, 0, 0, dur, 0.0);
        }
        catch (Exception ex)
        {
            Errors.Add(1);
            act?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _log.LogError(ex, "Errore in LlamaSharpProvider.RefineAsync");
            var dur = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            LatencyMs.Record(dur);
            return (new JsonArray(), "local", 0, 0, dur, 0.0);
        }
    }

    private static JsonArray? ExtractFirstJsonArray(string text)
    {
        int depth = 0; int start = -1;
        for (int i=0;i<text.Length;i++)
        {
            if (text[i]=='[') { if (depth==0) start = i; depth++; }
            else if (text[i]==']') { depth--; if (depth==0 && start>=0) { var seg = text.Substring(start, i-start+1); try { var n = System.Text.Json.JsonNode.Parse(seg) as System.Text.Json.Nodes.JsonArray; return n; } catch { return null; } } }
        }
        return null;
    }
}
