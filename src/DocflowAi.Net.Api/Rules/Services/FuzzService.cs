using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Services;

/// <summary>
/// Builds fuzzing test cases by probing numeric boundaries and missing fields.
/// </summary>
public class FuzzService
{
    private readonly IRuleFunctionRepository _rules;
    private readonly IRuleTestCaseRepository _tests;

    public FuzzService(IRuleFunctionRepository rules, IRuleTestCaseRepository tests)
    {
        _rules = rules;
        _tests = tests;
    }

    public Task<JsonArray> GenerateAsync(Guid ruleId, int maxPerField, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule == null) return Task.FromResult(new JsonArray());

        var fields = AnalyzeFields(rule.Code ?? string.Empty);
        var arr = new JsonArray();
        foreach (var f in fields)
        {
            var nums = new[] { -1, 0, 1, 9, 10, 99, 100, 101, 1000 };
            int c = 0;
            foreach (var n in nums)
            {
                if (c >= maxPerField) break;
                var jo = new JsonObject
                {
                    ["name"] = $"{f} fuzz {n}",
                    ["suite"] = "fuzz",
                    ["tags"] = new JsonArray("fuzz"),
                    ["input"] = new JsonObject { [f] = n },
                    ["expect"] = new JsonObject { ["fields"] = new JsonObject() }
                };
                arr.Add(jo); c++;
            }
            if (c < maxPerField)
            {
                arr.Add(new JsonObject
                {
                    ["name"] = $"{f} missing",
                    ["suite"] = "fuzz",
                    ["tags"] = new JsonArray("fuzz", "nullability"),
                    ["input"] = new JsonObject(),
                    ["expect"] = new JsonObject { ["fields"] = new JsonObject() }
                });
            }
        }
        return Task.FromResult(arr);
    }

    public Task<int> ImportAsync(Guid ruleId, JsonArray payload, string? suite, string[]? tags, CancellationToken ct)
    {
        int count = 0;
        foreach (var item in payload)
        {
            if (item is not JsonObject jo) continue;
            var t = new RuleTestCase
            {
                RuleFunctionId = ruleId,
                Name = jo["name"]?.ToString() ?? $"fuzz-{Guid.NewGuid():N}",
                InputJson = (jo["input"] ?? new JsonObject()).ToJsonString(),
                ExpectJson = (jo["expect"] ?? new JsonObject()).ToJsonString(),
                Suite = suite ?? jo["suite"]?.ToString() ?? "fuzz",
                TagsCsv = string.Join(",", tags?.Length > 0 ? tags : jo["tags"]?.AsArray()?.Select(x => x!.ToString()).ToArray() ?? new[] { "fuzz" }),
                Priority = 3,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _tests.Add(t); count++;
        }
        _tests.SaveChanges();
        return Task.FromResult(count);
    }

    private static IEnumerable<string> AnalyzeFields(string code)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(code, @"(has|missing|get(?:<[^>]+>)?)\(""(?<f>[A-Za-z0-9_]+)""\)"))
        {
            fields.Add(m.Groups["f"].Value);
        }
        return fields;
    }
}
