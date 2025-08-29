using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DocflowRules.Sdk;

public interface IScriptRunner
{
    Task<(bool ok, string[] errors)> CompileAsync(string code, CancellationToken ct);
    Task<(JsonObject before, JsonObject after, JsonArray mutations, long durationMs, string[] logs)> RunAsync(string code, JsonObject input, CancellationToken ct);
}

public static class DocflowRulesCoreExtensions
{
    public static IServiceCollection AddDocflowRulesCore(this IServiceCollection services)
    {
        services.AddSingleton<IScriptRunner, RoslynScriptRunner>();
        services.AddSingleton<IRuleEngine, RuleEngine>();
        return services;
    }
}

public interface IRuleEngine
{
    Task<RunResult> RunAsync(string code, JsonObject input, CancellationToken ct);
}

public record RunResult(JsonObject Before, JsonObject After, JsonArray Mutations, long DurationMs, string[] Logs);

public sealed class RuleEngine : IRuleEngine
{
    private readonly IScriptRunner _runner;
    public RuleEngine(IScriptRunner runner) => _runner = runner;
    public async Task<RunResult> RunAsync(string code, JsonObject input, CancellationToken ct)
    {
        var (b, a, m, d, l) = await _runner.RunAsync(code, input, ct);
        return new RunResult(b, a, m, d, l);
    }
}

public sealed class RoslynScriptRunner : IScriptRunner
{
    private static readonly ScriptOptions _opts = ScriptOptions.Default
        .WithReferences(typeof(ScriptGlobals).Assembly, typeof(object).Assembly)
        .WithImports("System","System.Linq");

    private readonly Dictionary<string, Func<ScriptGlobals, Task>> _cache = new();

    public Task<(bool ok, string[] errors)> CompileAsync(string code, CancellationToken ct)
    {
        var script = CSharpScript.Create(code, _opts, typeof(ScriptGlobals));
        var diags = script.Compile();
        var errs = diags.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray();
        return Task.FromResult((errs.Length == 0, errs));
    }

    public async Task<(JsonObject before, JsonObject after, JsonArray mutations, long durationMs, string[] logs)> RunAsync(string code, JsonObject input, CancellationToken ct)
    {
        var (fields, meta) = ParseInput(input);
        var ctx = new ExtractionContext(fields, meta);
        var globals = new ScriptGlobals { Ctx = ctx };

        var key = Hash(code);
        if (!_cache.TryGetValue(key, out var del))
        {
            var script = CSharpScript.Create(code, _opts, typeof(ScriptGlobals));
            var diags = script.Compile();
            if (diags.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
                throw new Exception("Compilation error: " + string.Join("\n", diags.Select(d => d.ToString())));
            var dlg = script.CreateDelegate();
            del = async g => await dlg(g);
            _cache[key] = del;
        }

        var before = ctx.ToJson();
        var sw = Stopwatch.StartNew();
        var logs = new List<string>();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));
            await del(globals);
        }
        catch (Exception ex)
        {
            logs.Add(ex.GetType().Name + ": " + ex.Message);
        }
        sw.Stop();
        var after = ctx.ToJson();
        var diff = ctx.DiffSince(before);
        return (before, after, diff, sw.ElapsedMilliseconds, logs.ToArray());
    }

    private static string Hash(string code)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(code)));
    }

    private static (Dictionary<string, FieldValue>, JsonObject) ParseInput(JsonObject input)
    {
        var fields = new Dictionary<string, FieldValue>(StringComparer.OrdinalIgnoreCase);
        var meta = new JsonObject();
        if (input.TryGetPropertyValue("fields", out var fnode) && fnode is JsonObject fo)
        {
            foreach (var kv in fo)
            {
                if (kv.Value is JsonObject vo)
                {
                    vo.TryGetPropertyValue("value", out var v);
                    var conf = vo.TryGetPropertyValue("confidence", out var c) && c is not null ? (double)c! : 1.0;
                    var src = vo.TryGetPropertyValue("source", out var s) && s is not null ? s!.ToString() : "input";
                    fields[kv.Key] = new FieldValue(v, conf, src);
                }
            }
        }
        if (input.TryGetPropertyValue("meta", out var mnode) && mnode is JsonObject mo)
            meta = mo;
        return (fields, meta);
    }
}
