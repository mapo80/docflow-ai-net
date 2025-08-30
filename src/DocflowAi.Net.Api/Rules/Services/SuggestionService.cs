using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Linq;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Validation;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Rules.Services;

public class SuggestionService
{
    private readonly IRuleFunctionRepository _rules;
    private readonly IRuleTestCaseRepository _tests;
    private readonly ISuggestedTestRepository _suggs;
    private readonly TestUpsertValidator _validator;
    private readonly ILogger<SuggestionService> _log;

    public SuggestionService(IRuleFunctionRepository rules, IRuleTestCaseRepository tests, ISuggestedTestRepository suggs, TestUpsertValidator validator, ILogger<SuggestionService> log)
    { _rules = rules; _tests = tests; _suggs = suggs; _validator = validator; _log = log; }

    public Task<(List<SuggestedTest> Suggs, string Model, int Total, int InTok, int OutTok, long DurMs, double CostUsd)> SuggestAsync(Guid ruleId, string? userPrompt, int budget, double temperature, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule == null) return Task.FromResult((new List<SuggestedTest>(), "static-v1", 0, 0, 0, 0L, 0.0));
        var skeletons = Analyze(rule.Code ?? "");

        var existing = _tests.GetByRule(ruleId);
        var covered = new HashSet<string>(existing.SelectMany(t => ExtractFields(t.ExpectJson)));

        var res = new List<SuggestedTest>();
        foreach (var s in skeletons.Take(budget))
        {
            var payload = new TestUpsertPayload { Name = s.name, Input = s.input, Expect = s.expect, Suite = s.suite, Tags = s.tags, Priority = s.priority };
            var val = _validator.Validate(payload);
            if (!val.IsValid) continue;

            var fields = (payload.Expect["fields"] as JsonObject) ?? new();
            var delta = new List<object>();
            int score = 0;
            foreach (var kv in fields)
            {
                var isNew = covered.Add(kv.Key);
                if (isNew) score++;
                delta.Add(new { field = kv.Key, delta = isNew ? 1 : 0 });
            }

            var hashInput = (rule.Code ?? "") + payload.Expect.ToJsonString();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput)));
            if (_suggs.Exists(ruleId, hash)) continue;

            var sug = new SuggestedTest
            {
                RuleId = ruleId,
                PayloadJson = JsonSerializer.Serialize(payload),
                Reason = s.reason,
                CoverageDeltaJson = JsonSerializer.Serialize(delta),
                Score = score,
                Hash = hash,
                Model = "static-v1"
            };
            _suggs.Add(sug);
            res.Add(sug);
        }
        _suggs.SaveChanges();
        return Task.FromResult((res, "static-v1", skeletons.Count, 0, 0, 0L, 0.0));
    }

    public Task<int> ImportAsync(Guid ruleId, IEnumerable<Guid> ids, string? suite, string[]? tags, CancellationToken ct)
    {
        var list = _suggs.GetByIds(ruleId, ids);
        int count = 0;
        foreach (var s in list)
        {
            var payload = JsonSerializer.Deserialize<TestUpsertPayload>(s.PayloadJson)!;
            var tc = new RuleTestCase
            {
                Id = Guid.NewGuid(),
                RuleFunctionId = ruleId,
                Name = payload.Name,
                InputJson = payload.Input.ToJsonString(),
                ExpectJson = payload.Expect.ToJsonString(),
                Suite = suite ?? payload.Suite ?? "ai",
                TagsCsv = string.Join(",", tags?.Length > 0 ? tags : payload.Tags ?? new[] { "ai" }),
                Priority = payload.Priority ?? 3,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _tests.Add(tc);
            count++;
        }
        _tests.SaveChanges();
        return Task.FromResult(count);
    }

    private static List<(string reason, JsonObject input, JsonObject expect, string? suite, string[] tags, int? priority, string name)> Analyze(string code)
    {
        var list = new List<(string, JsonObject, JsonObject, string?, string[], int?, string)>();
        foreach (Match m in Regex.Matches(code, "new Regex\\(\"(?<pat>[^\"]+)\""))
        {
            var pat = m.Groups["pat"].Value;
            var expect = new JsonObject { ["fields"] = new JsonObject { ["text"] = new JsonObject { ["regex"] = pat } } };
            list.Add(($"matches {pat}", new JsonObject(), expect, "regex", new[] { "regex", "ai" }, 3, $"Regex {pat}"));
        }
        foreach (Match m in Regex.Matches(code, @"(?<field>[A-Za-z_][A-Za-z0-9_]*)\s*>\s*(?<num>\d+(?:\.\d+)?)"))
        {
            var field = m.Groups["field"].Value;
            var num = double.Parse(m.Groups["num"].Value);
            var tol = Math.Max(0.01, Math.Abs(num) * 0.01);
            var expect = new JsonObject { ["fields"] = new JsonObject { [field] = new JsonObject { ["approx"] = num, ["tol"] = tol } } };
            var input = new JsonObject { [field] = num + tol };
            list.Add(($"covers {field} > {num}", input, expect, "boundaries", new[] { "boundary", "ai" }, 2, $"Boundary {field} {num}"));
        }
        if (list.Count == 0)
        {
            var expect = new JsonObject { ["fields"] = new JsonObject() };
            list.Add(("baseline", new JsonObject(), expect, "ai", new[] { "ai" }, 3, "Baseline"));
        }
        return list;
    }

    private static IEnumerable<string> ExtractFields(string? expectJson)
    {
        if (string.IsNullOrEmpty(expectJson)) return Enumerable.Empty<string>();
        try
        {
            var obj = JsonNode.Parse(expectJson) as JsonObject;
            var fields = obj?["fields"] as JsonObject;
            return fields?.Select(kv => kv.Key) ?? Enumerable.Empty<string>();
        }
        catch { return Enumerable.Empty<string>(); }
    }
}
