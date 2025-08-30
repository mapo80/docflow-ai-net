using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Runtime;

namespace DocflowAi.Net.Api.Rules.Services;

public record PropertyFailure(string Property, JsonObject Counterexample, string Message);
public record PropertyRunResult(int Trials, int Passed, int Failed, List<PropertyFailure> Failures);

public class PropertyTestService
{
    private readonly IRuleFunctionRepository _rules;
    private readonly IRuleTestCaseRepository _tests;
    private readonly IScriptRunner _runner;

    public PropertyTestService(IRuleFunctionRepository rules, IRuleTestCaseRepository tests, IScriptRunner runner)
    {
        _rules = rules;
        _tests = tests;
        _runner = runner;
    }

    public async Task<PropertyRunResult> RunForRuleAsync(Guid ruleId, int trials, int? seed, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule is null)
            return new PropertyRunResult(0, 0, 0, new());
        return await RunCoreAsync(rule.Code, null, trials, seed ?? Environment.TickCount, ct);
    }

    public Task<PropertyRunResult> RunFromBlocksAsync(JsonArray blocks, int trials, int? seed, CancellationToken ct) =>
        RunCoreAsync(string.Empty, blocks, trials, seed ?? Environment.TickCount, ct);

    public Task<int> ImportFailuresAsync(Guid ruleId, IEnumerable<PropertyFailure> failures, string? suite, string[]? tags, CancellationToken ct)
    {
        int count = 0;
        foreach (var f in failures ?? Array.Empty<PropertyFailure>())
        {
            var t = new RuleTestCase
            {
                RuleFunctionId = ruleId,
                Name = $"propfail - {f.Property} - {DateTimeOffset.UtcNow:HHmmss}-{count + 1}",
                InputJson = (f.Counterexample ?? new JsonObject()).ToJsonString(),
                ExpectJson = new JsonObject().ToJsonString(),
                Suite = suite ?? "property-fails",
                TagsCsv = string.Join(",", (tags?.Length > 0 ? tags : new[] { "property", f.Property })),
                Priority = 2,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _tests.Add(t);
            count++;
        }
        _tests.SaveChanges();
        return Task.FromResult(count);
    }

    private async Task<PropertyRunResult> RunCoreAsync(string code, JsonArray? blocks, int trials, int seed, CancellationToken ct)
    {
        var rnd = new Random(seed);
        var failures = new List<PropertyFailure>();
        int passed = 0, failed = 0;
        var types = InferFieldTypes(code);
        if (blocks is not null)
        {
            foreach (var b in blocks)
            {
                if (b is JsonObject o)
                {
                    if (o["field"] is JsonNode f) types.TryAdd(f.ToString(), "string");
                    if (o["target"] is JsonNode t) types.TryAdd(t.ToString(), "string");
                }
            }
        }

        for (int i = 0; i < trials; i++)
        {
            var input = new JsonObject();
            var fields = new JsonObject();
            foreach (var kv in types)
            {
                if (kv.Value == "number")
                    fields[kv.Key] = new JsonObject { ["value"] = rnd.NextDouble() * 100 };
                else if (kv.Value == "date")
                    fields[kv.Key] = new JsonObject { ["value"] = DateTimeOffset.FromUnixTimeSeconds(rnd.Next(0, 2000000000)).ToString("O") };
                else
                    fields[kv.Key] = new JsonObject { ["value"] = Guid.NewGuid().ToString("N") };
            }
            if (fields.Count == 0)
                fields["text"] = new JsonObject { ["value"] = Guid.NewGuid().ToString("N") };
            input["fields"] = fields;

            try
            {
                var r1 = await _runner.RunAsync(code, input, ct);
                var after1 = r1.after;
                var r2 = await _runner.RunAsync(code, after1, ct);
                var after2 = r2.after;
                if (after1.ToJsonString() != after2.ToJsonString())
                {
                    failed++; failures.Add(new("idempotence", input, "after != after(after)")); continue;
                }
                if (blocks is not null)
                {
                    foreach (var b in blocks)
                    {
                        var o = b!.AsObject();
                        var type = o["type"]!.ToString();
                        if (type == "set")
                        {
                            var src = o["field"]!.ToString();
                            var tgt = o["target"]!.ToString();
                            var v = after2["fields"]?.AsObject()?[tgt]?.AsObject()?["value"];
                            var s = after2["fields"]?.AsObject()?[src]?.AsObject()?["value"];
                            if ((v is null && s is not null) || (v is not null && s is not null && v!.ToJsonString() != s!.ToJsonString()))
                            { failed++; failures.Add(new("set_copies_source", input, $"target '{tgt}' should equal source '{src}'")); goto NEXT; }
                        }
                        else if (type == "normalize" && o["kind"]?.ToString() == "number")
                        {
                            var f = o["field"]!.ToString();
                            var vv = after2["fields"]?.AsObject()?[f]?.AsObject()?["value"];
                            if (vv is not null && !double.TryParse(vv!.ToString(), out _))
                            { failed++; failures.Add(new("normalize_number_parseable", input, $"field '{f}' should be numeric")); goto NEXT; }
                        }
                        else if (type == "normalize" && o["kind"]?.ToString() == "date")
                        {
                            var f = o["field"]!.ToString();
                            var vv = after2["fields"]?.AsObject()?[f]?.AsObject()?["value"];
                            if (vv is not null && !DateTimeOffset.TryParse(vv!.ToString(), out _))
                            { failed++; failures.Add(new("normalize_date_parseable", input, $"field '{f}' should be date ISO")); goto NEXT; }
                        }
                    }
                }
                passed++;
                NEXT:;
            }
            catch (Exception ex)
            {
                failed++; failures.Add(new("no_throw", new JsonObject { ["input"] = input }, ex.Message));
            }
        }
        return new PropertyRunResult(trials, passed, failed, failures);
    }

    private static Dictionary<string, string> InferFieldTypes(string code)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(code, "\\b([A-Za-z_][A-Za-z0-9_]*)\\b\\s*(?:[+\\-*/]|[<>]=?|==|!=)\\s*(?:\\d+\\.?\\d*)"))
            map[m.Groups[1].Value] = "number";
        foreach (Match m in Regex.Matches(code, "DateTime(?:Offset)?\\.TryParse\\([^\\)]*\"([A-Za-z_][A-Za-z0-9_]*)\""))
            map[m.Groups[1].Value] = "date";
        foreach (Match m in Regex.Matches(code, "Regex\\.IsMatch\\(.*\"([A-Za-z_][A-Za-z0-9_]*)\""))
            if (!map.ContainsKey(m.Groups[1].Value)) map[m.Groups[1].Value] = "string";
        foreach (Match m in Regex.Matches(code, "Get<[^>]+>\\(\"([A-Za-z_][A-Za-z0-9_]*)\"\\)"))
        {
            var f = m.Groups[1].Value;
            if (!map.ContainsKey(f)) map[f] = "string";
        }
        return map;
    }
}
