using System.Text.Json.Nodes;
using DocflowRules.Sdk;

namespace DocflowRules.Api.Runners;

public sealed class HttpScriptRunnerClient : IScriptRunner
{
    private readonly HttpClient _http;
    public HttpScriptRunnerClient(HttpClient http) { _http = http; }

    public async Task<(bool ok, string[] errors)> CompileAsync(string code, CancellationToken ct)
    {
        var r = await _http.PostAsJsonAsync("/compile", new { code }, ct);
        r.EnsureSuccessStatusCode();
        var json = await r.Content.ReadFromJsonAsync<CompileResp>(cancellationToken: ct);
        return (json?.ok ?? false, json?.errors ?? Array.Empty<string>());
    }

    public async Task<(JsonObject before, JsonObject after, JsonArray mutations, long durationMs, string[] logs)> RunAsync(string code, JsonObject input, CancellationToken ct)
    {
        var r = await _http.PostAsJsonAsync("/run", new { code, input }, ct);
        r.EnsureSuccessStatusCode();
        var json = await r.Content.ReadFromJsonAsync<RunResp>(cancellationToken: ct) ?? throw new InvalidOperationException();
        return (json.before!, json.after!, json.mutations!, json.durationMs, json.logs ?? Array.Empty<string>());
    }

    private record CompileResp(bool ok, string[] errors);
    private record RunResp(System.Text.Json.Nodes.JsonObject before, System.Text.Json.Nodes.JsonObject after, System.Text.Json.Nodes.JsonArray mutations, long durationMs, string[] logs);
}
