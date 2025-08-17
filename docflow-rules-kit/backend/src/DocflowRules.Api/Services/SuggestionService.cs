using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DocflowRules.Api.Validation;
using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public interface ILLMProvider
{
    Task<(JsonArray Refined, string Model, int InputTokens, int OutputTokens, long DurationMs, double CostUsd)> RefineAsync(string ruleName, string code, JsonArray skeletons, string? userPrompt, int budget, double temperature, CancellationToken ct);
}

public class MockLLMProvider : ILLMProvider
{
    public Task<(JsonArray Refined, string Model, int InputTokens, int OutputTokens, long DurationMs, double CostUsd)> RefineAsync(string ruleName, string code, JsonArray skeletons, string? userPrompt, int budget, double temperature, CancellationToken ct)
    {
        var start = DateTimeOffset.UtcNow;
        var arr = new JsonArray();
        foreach (var s in skeletons.Take(budget).Select(x => x!.AsObject()))
        {
            var clone = JsonNode.Parse(s.ToJsonString())!.AsObject();
            clone["name"] = $"{clone["name"]?.GetValue<string>()} • refined";
            arr.Add(clone);
        }
        var dur = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
        return Task.FromResult((arr, "mock-refine", 0, 0, dur, 0.0));
    }
}

public class StaticAnalyzer
{
    // Heuristic extraction of fields/thresholds/regex
    public (HashSet<string> fields, List<(string reason, JsonObject test)> tests) Analyze(string code)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tests = new List<(string, JsonObject)>();

        // find regex patterns
        foreach (Match m in Regex.Matches(code, @"new Regex\(\s*\"(?<pat>[^\"]+)\""))
        {
            var pat = m.Groups["pat"].Value;
            var name = $"Regex match {pat}";
            var t = new JsonObject {
                ["name"] = name,
                ["input"] = new JsonObject(),
                ["expect"] = new JsonObject { ["fields"] = new JsonObject { ["text"] = new JsonObject { ["regex"] = pat } } },
                ["suite"] = "regex",
                ["tags"] = new JsonArray("regex","ai"),
                ["priority"] = 3
            };
            tests.Add(($"riconosce pattern /{pat}/", t));
            fields.Add("text");
        }

        // numeric thresholds: x > 10, x >= 10, x < 10, etc.
        foreach (Match m in Regex.Matches(code, @"(?<field>[A-Za-z_][A-Za-z0-9_]*)\s*(?<op>>=|<=|>|<|==)\s*(?<num>\d+(?:\.\d+)?)"))
        {
            var f = m.Groups["field"].Value;
            var op = m.Groups["op"].Value;
            var num = double.Parse(m.Groups["num"].Value, System.Globalization.CultureInfo.InvariantCulture);
            fields.Add(f);

            double eps = Math.Max(0.01, Math.Abs(num)*0.01);
            var cases = new(double val, string name, string reason)[] {
                (num, $"Boundary {f} {op} {num}", $"copre soglia {f} {op} {num}"),
                (num + eps, $"Just over {f} {num}", $"sopra soglia {f}"),
                (num - eps, $"Just under {f} {num}", $"sotto soglia {f}")
            };
            foreach (var c in cases)
            {
                var t = new JsonObject {
                    ["name"] = c.name,
                    ["input"] = new JsonObject { [f] = c.val },
                    ["expect"] = new JsonObject { ["fields"] = new JsonObject { [f] = new JsonObject { ["approx"] = num, ["tol"] = eps } } },
                    ["suite"] = "boundaries",
                    ["tags"] = new JsonArray("boundary","numeric","ai"),
                    ["priority"] = 2
                };
                tests.Add((c.reason, t));
            }
        }

        // existence checks: if (x != null) or string.IsNullOrEmpty(x)
        foreach (Match m in Regex.Matches(code, @"(?:(?<f>[A-Za-z_][A-Za-z0-9_]*)\s*!=\s*null)|(string\.IsNullOrEmpty\((?<f>[A-Za-z_][A-Za-z0-9_]*)\))"))
        {
            var f = m.Groups["f"].Value;
            if (string.IsNullOrWhiteSpace(f)) continue;
            fields.Add(f);
            var t1 = new JsonObject {
                ["name"] = $"Exists {f}",
                ["input"] = new JsonObject { [f] = "value" },
                ["expect"] = new JsonObject { ["fields"] = new JsonObject { [f] = new JsonObject { ["exists"] = true } } },
                ["suite"] = "nullability",
                ["tags"] = new JsonArray("exists","ai"),
                ["priority"] = 3
            };
            var t2 = new JsonObject {
                ["name"] = $"Not exists {f}",
                ["input"] = new JsonObject { },
                ["expect"] = new JsonObject { ["fields"] = new JsonObject { [f] = new JsonObject { ["exists"] = false } } },
                ["suite"] = "nullability",
                ["tags"] = new JsonArray("exists","ai"),
                ["priority"] = 3
            };
            tests.Add(($"presenza campo {f}", t1));
            tests.Add(($"assenza campo {f}", t2));
        }

        if (tests.Count == 0)
        {
            // baseline
            var t = new JsonObject {
                ["name"] = "Happy path (baseline)",
                ["input"] = new JsonObject(),
                ["expect"] = new JsonObject { ["fields"] = new JsonObject() },
                ["suite"] = "smoke",
                ["tags"] = new JsonArray("smoke","ai"),
                ["priority"] = 3
            };
            tests.Add(("baseline", t));
        }

        return (fields, tests);
    }
}

public class SuggestionService
{
    private readonly AppDbContext _db;
    private readonly TestUpsertValidator _validator;
    private readonly ILLMProviderRegistry _reg; private readonly ILlmConfigService _cfgSvc;
    private readonly ILogger<SuggestionService> _log;
    public SuggestionService(AppDbContext db, TestUpsertValidator validator, ILLMProviderRegistry reg, ILlmConfigService cfgSvc, ILogger<SuggestionService> log)
    {
        _db = db; _validator = validator; _reg = reg; _cfgSvc = cfgSvc; _log = log;
    }

    public async Task<(IReadOnlyList<SuggestedTest> suggestions, string model, int totalSkeletons, int inputTokens, int outputTokens, long durationMs, double costUsd)> SuggestAsync(Guid ruleId, string? userPrompt, int budget, double temperature, Guid? modelId, bool? turbo, CancellationToken ct)
    {
        // TTL cleanup
        var ttl = DateTimeOffset.UtcNow.AddDays(-1);
        var old = _db.SuggestedTests.Where(s => s.CreatedAt < ttl);
        _db.SuggestedTests.RemoveRange(old);
        await _db.SaveChangesAsync(ct);

        var rule = await _db.RuleFunctions.FirstOrDefaultAsync(r => r.Id == ruleId, ct);
        if (rule == null) throw new InvalidOperationException("Rule not found");

        
        // pick model config
        var (activeModel, activeTurbo) = await _cfgSvc.GetActiveWithTurboAsync(ct);
        var effectiveModel = modelId.HasValue ? await _cfgSvc.GetByIdAsync(modelId.Value, ct) : activeModel;
        if (effectiveModel == null) throw new InvalidOperationException("Nessun modello LLM configurato");
        var useTurbo = turbo ?? activeTurbo;

        var provider = _reg.GetProvider(effectiveModel.Provider);
        if (provider is ILLMProviderConfigurable conf)
        {
            conf.SetRuntimeConfig(effectiveModel, useTurbo);
        }

        var analyzer = new StaticAnalyzer();
        var (_, tests) = analyzer.Analyze(rule.Code ?? "");

        // skeletons JSON
        var skeletons = new JsonArray(tests.Select(t => t.test).ToArray());

        // LLM refine (mock provider by default)
        var llm = await provider.RefineAsync(rule.Name, rule.Code ?? "", skeletons, userPrompt, budget, useTurbo ? Math.Min(0.2, temperature) : temperature, ct);
        var refined = llm.Refined;
        var all = refined.Count > 0 ? refined : skeletons;
        var model = refined.Count > 0 ? llm.Model : "static-v1";

        // compute current covered fields from existing tests
        var existing = await _db.RuleTestCases.Where(t => t.RuleFunctionId == ruleId).ToListAsync(ct);
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tc in existing)
        {
            try
            {
                var exp = JsonNode.Parse(tc.ExpectJson ?? "{}") as JsonObject;
                var fields = (exp?["fields"] as JsonObject) ?? new JsonObject();
                foreach (var kv in fields) covered.Add(kv.Key);
            } catch {}
        }

        var res = new List<SuggestedTest>();
        foreach (var node in all)
        {
            if (node is not JsonObject jo) continue;

            // validate
            var payload = new TestUpsertPayload {
                Name = jo["name"]?.ToString() ?? "AI Test",
                Input = (jo["input"] as JsonObject) ?? new JsonObject(),
                Expect = (jo["expect"] as JsonObject) ?? new JsonObject(),
                Suite = jo["suite"]?.ToString(),
                Tags = jo["tags"] is JsonArray arr ? arr.Select(x=> x!.ToString()).ToArray() : new [] { "ai" },
                Priority = jo["priority"]?.GetValue<int?>()
            };
            var validate = _validator.Validate(payload);
            if (!validate.IsValid)
            {
                _log.LogWarning("AI suggestion invalid: {Errors}", string.Join("; ", validate.Errors.Select(e => e.ErrorMessage)));
                continue;
            }

            // coverage delta
            var delta = new List<object>();
            var fields = ((payload.Expect["fields"] as JsonObject) ?? new()).Select(kv => kv.Key).ToList();
            int newFields = 0;
            foreach (var f in fields)
            {
                var isNew = !covered.Contains(f);
                delta.Add(new { field = f, delta = isNew ? 1 : 0 });
                if (isNew) newFields++;
            }

            // dedupe by hash(ruleCode + payload)
            var hashInput = (rule.Code ?? "") + jo.ToJsonString();
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hashInput)));
            if (await _db.SuggestedTests.AnyAsync(s => s.RuleId == ruleId && s.Hash == hash, ct)) continue;

            var reason = jo["reason"]?.ToString();
            if (string.IsNullOrWhiteSpace(reason))
            {
                // derive simple reason
                reason = fields.Count > 0 ? $"Copre campi {string.Join(", ", fields)}" : "Caso generico";
            }

            var s = new SuggestedTest {
                RuleId = ruleId,
                PayloadJson = jo.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
                Reason = reason,
                CoverageDeltaJson = JsonSerializer.Serialize(delta),
                Score = 0.6 * newFields + 0.4 * fields.Count,
                Hash = hash,
                Model = model
            };
            _db.SuggestedTests.Add(s);
            res.Add(s);
        }
        await _db.SaveChangesAsync(ct);

        // order by score desc
        res = res.OrderByDescending(x => x.Score).Take(budget).ToList();
        return (res, model, tests.Count, refined.Count>0 ? llm.InputTokens : 0, refined.Count>0 ? llm.OutputTokens : 0, refined.Count>0 ? llm.DurationMs : 0, refined.Count>0 ? llm.CostUsd : 0.0);
    }

    public async Task<int> ImportAsync(Guid ruleId, IEnumerable<Guid> ids, string? suite, string[]? tags, CancellationToken ct)
    {
        var list = await _db.SuggestedTests.Where(s => s.RuleId == ruleId && ids.Contains(s.Id)).ToListAsync(ct);
        int count = 0;
        foreach (var s in list)
        {
            var jo = JsonNode.Parse(s.PayloadJson)!.AsObject();
            var t = new RuleTestCase {
                Id = Guid.NewGuid(),
                RuleFunctionId = ruleId,
                Name = jo["name"]?.ToString() ?? "AI Test",
                InputJson = (jo["input"] ?? new JsonObject()).ToJsonString(),
                ExpectJson = (jo["expect"] ?? new JsonObject()).ToJsonString(),
                Suite = suite ?? jo["suite"]?.ToString() ?? "ai",
                TagsCsv = string.Join(",", (tags?.Length>0 ? tags : ((jo["tags"] as JsonArray)?.Select(x=>x!.ToString()).ToArray() ?? new [] { "ai" }))!),
                Priority = jo["priority"]?.GetValue<int?>() ?? 3,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.RuleTestCases.Add(t);
            count++;
        }
        await _db.SaveChangesAsync(ct);
        return count;
    }
}
