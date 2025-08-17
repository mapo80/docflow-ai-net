using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using DocflowRules.Api.Services;

namespace DocflowRules.Api.LLM;


    static readonly ActivitySource Activity = new("DocflowRules.LLM");
    static readonly Meter Meter = new("DocflowRules.LLM");
    static readonly Counter<long> Calls = Meter.CreateCounter<long>("llm.calls");
    static readonly Counter<long> Errors = Meter.CreateCounter<long>("llm.errors");
    static readonly Counter<long> InTokens = Meter.CreateCounter<long>("llm.input_tokens");
    static readonly Counter<long> OutTokens = Meter.CreateCounter<long>("llm.output_tokens");
    static readonly Histogram<double> LatencyMs = Meter.CreateHistogram<double>("llm.latency.ms");
    static readonly Histogram<double> CostUsd = Meter.CreateHistogram<double>("llm.cost.usd");

public class OpenAiProvider : ILLMProvider, DocflowRules.Api.Services.ILLMProviderConfigurable
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;
    private DocflowRules.Storage.EF.LlmModel? _runtimeModel;
    private bool _turbo;
    private readonly ILogger<OpenAiProvider> _log;
    public OpenAiProvider(IHttpClientFactory http, IConfiguration cfg, ILogger<OpenAiProvider> log) { _http = http; _cfg = cfg; _log = log; }

    public void SetRuntimeConfig(DocflowRules.Storage.EF.LlmModel model, bool turbo) { _runtimeModel = model; _turbo = turbo; }
    public Task WarmupAsync(CancellationToken ct) => Task.CompletedTask;

    public async Task<(JsonArray Refined, string Model, int InputTokens, int OutputTokens, long DurationMs, double CostUsd)> RefineAsync(string ruleName, string code, JsonArray skeletons, string? userPrompt, int budget, double temperature = effTemp, CancellationToken ct)
    {
        var key = _runtimeModel?.ApiKey ?? _cfg["LLM:ApiKey"];
        var endpoint = _runtimeModel?.Endpoint ?? _cfg["LLM:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        var model = _runtimeModel?.ModelPathOrId ?? _cfg["LLM:Model"] ?? "gpt-4o-mini";

        var maxAttempts = int.TryParse(_cfg["LLM:Retry:MaxAttempts"], out var ma) ? Math.Max(1, ma) : 3;
        var baseDelay = int.TryParse(_cfg["LLM:Retry:BaseDelayMs"], out var bd) ? Math.Max(0, bd) : 200;
        var maxDelay = int.TryParse(_cfg["LLM:Retry:MaxDelayMs"], out var md) ? Math.Max(0, md) : 4000;
        var jitterMs = int.TryParse(_cfg["LLM:Retry:JitterMs"], out var jm) ? Math.Max(0, jm) : 250;

        using var act = Activity.StartActivity("LLM.Refine", ActivityKind.Client);
        act?.SetTag("llm.provider", "openai");
        act?.SetTag("llm.model", model);
        act?.SetTag("llm.endpoint", endpoint);
        act?.SetTag("llm.rule.name", ruleName);
        act?.SetTag("llm.budget", budget);
        act?.SetTag("llm.temperature", temperature);

        Calls.Add(1, new KeyValuePair<string, object?>("model", model));

        if (string.IsNullOrWhiteSpace(key))
        {
            _log.LogWarning("LLM:ApiKey not configured. Falling back to empty result.");
            act?.SetStatus(ActivityStatusCode.Error, "missing_api_key");
            Errors.Add(1, new KeyValuePair<string, object?>("reason","missing_api_key"));
            return (new JsonArray(), model, 0, 0, 0, 0);
        }

        var sys = @"
Sei un assistente che genera casi di test per funzioni di validazione/normalizzazione documentale.
RESTIUISCI SOLO JSON. Lo schema è un array di oggetti con: name, input (object), expect.fields (object di regole equals/approx/regex/exists), suite, tags[], priority (1..5). 
Usa equals/approx/regex/exists in modo appropriato. Evita campi non presenti. Non inserire commenti.
";
        var usr = new
        {
            instruction = "Genera fino a " + budget + " test di alta qualità. Se utile, riusa e raffina gli skeleton di partenza. Se non puoi migliorare, restituisci gli skeleton così come sono.",
            ruleName,
            code = code.Length > 12000 ? code.Substring(0, 12000) : code,
            skeletons = JsonNode.Parse(skeletons.ToJsonString()),
            userPrompt = userPrompt
        };

        var effTemp = _turbo ? Math.Min(0.2, temperature) : temperature;
        var payload = new
        {
            model,
            temperature = effTemp,
            messages = new object[] {
                new { role = "system", content = sys },
                new { role = "user", content = System.Text.Json.JsonSerializer.Serialize(usr) }
            },
            n = 1,
            response_format = new { type = "json_object" }
        };

        var http = _http.CreateClient("openai");
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        var reqJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var req = new StringContent(reqJson, Encoding.UTF8, "application/json");

        var start = DateTimeOffset.UtcNow;
        int attempt = 0;
        int inTok = 0, outTok = 0;
        string? lastErr = null;
        while (true)
        {
            attempt++;
            act?.AddEvent(new ActivityEvent("attempt", tags: new ActivityTagsCollection { { "attempt", attempt } }));
            try
            {
                using var resp = await http.PostAsync(endpoint, req, ct);
                var text = await resp.Content.ReadAsStringAsync(ct);
                act?.SetTag("http.status_code", (int)resp.StatusCode);

                using var doc = System.Text.Json.JsonDocument.Parse(text);
                if (!resp.IsSuccessStatusCode)
                {
                    lastErr = $"status {(int)resp.StatusCode}";
                    act?.AddEvent(new ActivityEvent("error", tags: new ActivityTagsCollection { { "message", text } }));
                }
                else
                {
                    var root = doc.RootElement;
                    var usage = root.GetProperty("usage");
                    inTok = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                    outTok = usage.TryGetProperty("completion_tokens", out var ctok) ? ctok.GetInt32() : 0;
                    var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "[]";

                    JsonArray arr;
                    try
                    {
                        var node = JsonNode.Parse(content);
                        if (node is JsonArray ja) arr = ja;
                        else if (node is JsonObject jo && jo["suggestions"] is JsonArray ja2) arr = ja2;
                        else arr = new JsonArray();
                    }
                    catch (Exception ex)
                    {
                        lastErr = "parse_json";
                        act?.AddEvent(new ActivityEvent("error", tags: new ActivityTagsCollection { { "message", ex.Message } }));
                        arr = new JsonArray();
                    }

                    var dur = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
                    double cost = 0;
                    if (double.TryParse(_cfg["LLM:InputCostPer1K"], out var cin) && double.TryParse(_cfg["LLM:OutputCostPer1K"], out var cout))
                    {
                        cost = (inTok/1000.0)*cin + (outTok/1000.0)*cout;
                    }

                    act?.SetTag("llm.input_tokens", inTok);
                    act?.SetTag("llm.output_tokens", outTok);
                    act?.SetTag("llm.duration_ms", dur);
                    act?.SetTag("llm.cost_usd", cost);
                    act?.SetStatus(ActivityStatusCode.Ok);

                    InTokens.Add(inTok, new KeyValuePair<string, object?>("model", model));
                    OutTokens.Add(outTok, new KeyValuePair<string, object?>("model", model));
                    LatencyMs.Record(dur, new KeyValuePair<string, object?>("model", model));
                    CostUsd.Record(cost, new KeyValuePair<string, object?>("model", model));

                    return (arr, model, inTok, outTok, dur, cost);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                lastErr = ex.GetType().Name;
                act?.AddEvent(new ActivityEvent("error", tags: new ActivityTagsCollection { { "message", ex.Message } }));
            }

            if (attempt >= maxAttempts)
            {
                var dur = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
                act?.SetStatus(ActivityStatusCode.Error, lastErr ?? "failed");
                Errors.Add(1, new KeyValuePair<string, object?>("reason", lastErr ?? "failed"));
                LatencyMs.Record(dur, new KeyValuePair<string, object?>("model", model));
                return (new JsonArray(), model, inTok, outTok, dur, 0);
            }

            // backoff with jitter
            var delay = Math.Min(maxDelay, baseDelay * (int)Math.Pow(2, attempt-1));
            delay += new System.Random().Next(0, Math.Max(1, jitterMs));
            act?.AddEvent(new ActivityEvent("retry", tags: new ActivityTagsCollection { { "delay_ms", delay } }));
            await Task.Delay(delay, ct);
        }
    }