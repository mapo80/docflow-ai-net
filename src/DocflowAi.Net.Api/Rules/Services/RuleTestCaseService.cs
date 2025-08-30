using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.Generic;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Runtime;

namespace DocflowAi.Net.Api.Rules.Services;

public record TestRunOutcome(Guid Id, string Name, bool Passed, long DurationMs, JsonArray Diff, JsonObject Actual, string[] Logs);

/// <summary>
/// Provides rule test case CRUD and execution operations backed solely by repositories.
/// </summary>
public class RuleTestCaseService
{
    private readonly IRuleTestCaseRepository _tests;
    private readonly IRuleFunctionRepository _rules;
    private readonly IRuleEngine _engine;

    public RuleTestCaseService(IRuleTestCaseRepository tests, IRuleFunctionRepository rules, IRuleEngine engine)
    {
        _tests = tests;
        _rules = rules;
        _engine = engine;
    }

    public Task<(int total, List<RuleTestCase> items)> GetAllAsync(Guid ruleId, string? search, string? suite, string? tag, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct)
    {
        var items = _tests.GetByRule(ruleId).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(suite))
            items = items.Where(t => t.Suite == suite).ToList();
        if (!string.IsNullOrWhiteSpace(tag))
            items = items.Where(t => (t.TagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        items = (sortBy, sortDir?.ToLowerInvariant()) switch
        {
            ("updatedAt", "desc") => items.OrderByDescending(t => t.UpdatedAt).ToList(),
            ("updatedAt", "asc") => items.OrderBy(t => t.UpdatedAt).ToList(),
            ("priority", "desc") => items.OrderByDescending(t => t.Priority).ToList(),
            ("priority", "asc") => items.OrderBy(t => t.Priority).ToList(),
            _ => items.OrderBy(t => t.Name).ToList(),
        };
        var total = items.Count;
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((total, pageItems));
    }

    public Task<RuleTestCase> CreateAsync(Guid ruleId, string name, JsonObject input, JsonObject expect, string? suite, string[]? tags, int? priority, CancellationToken ct)
    {
        var t = new RuleTestCase
        {
            RuleFunctionId = ruleId,
            Name = name,
            InputJson = input.ToJsonString(),
            ExpectJson = expect.ToJsonString(),
            Suite = suite,
            TagsCsv = tags == null ? null : string.Join(',', tags),
            Priority = priority ?? 1
        };
        _tests.Add(t);
        _tests.SaveChanges();
        return Task.FromResult(t);
    }

    public Task<bool> UpdateMetaAsync(Guid ruleId, Guid testId, string? name, string? suite, string[]? tags, int? priority, CancellationToken ct)
    {
        var t = _tests.GetByRule(ruleId).FirstOrDefault(x => x.Id == testId);
        if (t == null) return Task.FromResult(false);
        if (!string.IsNullOrWhiteSpace(name)) t.Name = name;
        if (suite != null) t.Suite = suite;
        if (tags != null) t.TagsCsv = string.Join(',', tags);
        if (priority.HasValue) t.Priority = priority.Value;
        _tests.Update(t);
        _tests.SaveChanges();
        return Task.FromResult(true);
    }

    public Task<RuleTestCase?> CloneAsync(Guid ruleId, Guid testId, string? newName, string? suite, string[]? tags, CancellationToken ct)
    {
        var src = _tests.GetByRule(ruleId).FirstOrDefault(x => x.Id == testId);
        if (src == null) return Task.FromResult<RuleTestCase?>(null);
        var clone = new RuleTestCase
        {
            RuleFunctionId = ruleId,
            Name = string.IsNullOrWhiteSpace(newName) ? src.Name + " (copy)" : newName!,
            InputJson = src.InputJson,
            ExpectJson = src.ExpectJson,
            Suite = suite ?? src.Suite,
            TagsCsv = tags != null && tags.Length > 0 ? string.Join(',', tags) : src.TagsCsv,
            Priority = src.Priority
        };
        _tests.Add(clone);
        _tests.SaveChanges();
        return Task.FromResult<RuleTestCase?>(clone);
    }

    public async Task<List<TestRunOutcome>?> RunAllAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule == null) return null;
        var tests = _tests.GetByRule(ruleId);
        var list = new List<TestRunOutcome>();
        foreach (var t in tests)
        {
            var input = JsonNode.Parse(t.InputJson) as JsonObject ?? new();
            var expect = JsonNode.Parse(t.ExpectJson) as JsonObject ?? new();
            var started = DateTimeOffset.UtcNow;
            string[] logs = Array.Empty<string>();
            try
            {
                var run = await _engine.RunAsync(rule.Code, input, ct);
                logs = run.Logs;
                var (passed, diff) = Compare(run.After, expect);
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                list.Add(new TestRunOutcome(t.Id, t.Name, passed, dur, diff, run.After, logs));
            }
            catch (Exception ex)
            {
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                list.Add(new TestRunOutcome(t.Id, t.Name, false, dur, new JsonArray { new JsonObject { ["error"] = ex.Message } }, new JsonObject(), logs));
            }
        }
        return list;
    }

    public async Task<List<TestRunOutcome>?> RunSelectedAsync(Guid ruleId, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule == null) return null;
        var idSet = ids?.ToHashSet() ?? new HashSet<Guid>();
        var tests = _tests.GetByRule(ruleId).Where(t => idSet.Contains(t.Id));
        var list = new List<TestRunOutcome>();
        foreach (var t in tests)
        {
            var input = JsonNode.Parse(t.InputJson) as JsonObject ?? new();
            var expect = JsonNode.Parse(t.ExpectJson) as JsonObject ?? new();
            var started = DateTimeOffset.UtcNow;
            string[] logs = Array.Empty<string>();
            try
            {
                var run = await _engine.RunAsync(rule.Code, input, ct);
                logs = run.Logs;
                var (passed, diff) = Compare(run.After, expect);
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                list.Add(new TestRunOutcome(t.Id, t.Name, passed, dur, diff, run.After, logs));
            }
            catch (Exception ex)
            {
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                list.Add(new TestRunOutcome(t.Id, t.Name, false, dur, new JsonArray { new JsonObject { ["error"] = ex.Message } }, new JsonObject(), logs));
            }
        }
        return list;
    }

    public async Task<List<object>?> CoverageAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = _rules.GetById(ruleId);
        if (rule == null) return null;
        var tests = _tests.GetByRule(ruleId);
        var summary = new Dictionary<string, (int tested, int mutated, int hits, int pass)>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tests)
        {
            var input = JsonNode.Parse(t.InputJson) as JsonObject ?? new();
            var expect = JsonNode.Parse(t.ExpectJson) as JsonObject ?? new();
            var expectFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (expect["fields"] is JsonObject fobj)
                foreach (var kv in fobj)
                    expectFields.Add(kv.Key);
            try
            {
                var run = await _engine.RunAsync(rule.Code, input, ct);
                var mutated = run.Mutations
                    .Select(m => (string?)m?["field"])
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var failedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in expectFields)
                {
                    var actual = run.After["fields"]?.AsObject()?[field];
                    var expected = expect["fields"]?.AsObject()?[field];
                    if (!JsonNode.DeepEquals(actual, expected))
                        failedFields.Add(field);
                }
                foreach (var field in expectFields.Union(mutated))
                {
                    var tested = expectFields.Contains(field) ? 1 : 0;
                    var mut = mutated.Contains(field) ? 1 : 0;
                    var hit = (tested == 1 && mut == 1) ? 1 : 0;
                    var pass = (tested == 1 && !failedFields.Contains(field)) ? 1 : 0;
                    summary[field] = summary.TryGetValue(field, out var s)
                        ? (s.tested + tested, s.mutated + mut, s.hits + hit, s.pass + pass)
                        : (tested, mut, hit, pass);
                }
            }
            catch { }
        }
        return summary.Select(kv => (object)new { field = kv.Key, kv.Value.tested, kv.Value.mutated, kv.Value.hits, kv.Value.pass })
                      .OrderByDescending(x => ((dynamic)x).hits)
                      .ThenBy(x => ((dynamic)x).field)
                      .ToList();
    }

    private static (bool passed, JsonArray diff) Compare(JsonObject actual, JsonObject expect)
    {
        var diff = new JsonArray();
        var pass = true;
        if (expect["fields"] is JsonObject fexp)
        {
            foreach (var kv in fexp)
            {
                var actualField = actual["fields"]?.AsObject()?[kv.Key];
                if (!JsonNode.DeepEquals(actualField, kv.Value))
                {
                    pass = false;
                    diff.Add(new JsonObject
                    {
                        ["field"] = kv.Key,
                        ["expected"] = kv.Value,
                        ["actual"] = actualField
                    });
                }
            }
        }
        return (pass, diff);
    }
}

