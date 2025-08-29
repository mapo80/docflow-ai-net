using System.Text.Json.Nodes;
using DocflowRules.Api.DTO;
using DocflowRules.Sdk;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rules/{ruleId:guid}/tests")]
public class RuleTestsController : ControllerBase
{
    private static readonly System.Diagnostics.ActivitySource _activitySource = new("DocflowRules");
    private static readonly System.Diagnostics.Metrics.Meter _meter = new("DocflowRules.Metrics");
    private static readonly System.Diagnostics.Metrics.Histogram<double> _testDurationMs = _meter.CreateHistogram<double>("test.duration.ms");
    private static readonly System.Diagnostics.Metrics.Counter<long> _testPass = _meter.CreateCounter<long>("test.pass.count");
    private static readonly System.Diagnostics.Metrics.Counter<long> _testFail = _meter.CreateCounter<long>("test.fail.count");
{
    private readonly ILogger<RuleTestsController> _log;
    private readonly IRuleTestCaseRepository _repoTests;
    private readonly IRuleFunctionRepository _repoRules;
    private readonly IRuleEngine _engine;

    public RuleTestsController(ILogger<RuleTestsController> log, IRuleTestCaseRepository repoTests, IRuleFunctionRepository repoRules, IRuleEngine engine)
    { _repoTests = repoTests; _repoRules = repoRules; _engine = engine; }

    [HttpGet]
    public async Task<ActionResult<object>> Get(Guid ruleId, [FromQuery] string? search, [FromQuery] string? suite, [FromQuery] string? tag, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _repoTests.GetByRuleAsync(ruleId, ct);

        static IEnumerable<DocflowRules.Storage.EF.RuleTestCase> ApplySort(IEnumerable<DocflowRules.Storage.EF.RuleTestCase> items, string? sortBy, string? sortDir, string? sort)
        {
            List<(string field, string dir)> sorts = new();
            if (!string.IsNullOrWhiteSpace(sort))
            {
                foreach (var s in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parts = s.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var f = parts.ElementAtOrDefault(0) ?? "name";
                    var d = parts.ElementAtOrDefault(1) ?? "asc";
                    sorts.Add((f.ToLowerInvariant(), d.ToLowerInvariant()));
                }
            }
            else if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sorts.Add((sortBy.ToLowerInvariant(), (sortDir ?? "asc").ToLowerInvariant()));
            }
            if (sorts.Count == 0) sorts.Add(("name","asc"));

            IOrderedEnumerable<DocflowRules.Storage.EF.RuleTestCase>? ordered = null;
            foreach (var (field, dir) in sorts)
            {
                Func<DocflowRules.Storage.EF.RuleTestCase, object?> key = field switch
                {
                    "updatedat" => x => x.UpdatedAt,
                    "priority" => x => x.Priority,
                    "suite" => x => x.Suite,
                    "tag" or "tags" => x => (x.TagsCsv ?? string.Empty),
                    _ => x => x.Name
                };
                if (ordered == null)
                    ordered = (dir == "desc") ? items.OrderByDescending(key) : items.OrderBy(key);
                else
                    ordered = (dir == "desc") ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
            }
            return ordered ?? items;
        }

        items = ApplySort(items, sortBy, sortDir, sort).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(suite))
            items = items.Where(x => x.Suite == suite).ToList();
        if (!string.IsNullOrWhiteSpace(tag))
            items = items.Where(x => (x.TagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(tag)).ToList();
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var arr = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var modeAnd = string.Equals((tagsMode ?? "or"), "and", StringComparison.OrdinalIgnoreCase);
            items = items.Where(x => {
                var xtags = (x.TagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (arr.Count == 0) return true;
                if (modeAnd) return arr.All(t => xtags.Contains(t));
                else return arr.Any(t => xtags.Contains(t));
            }).ToList();
        }
        items = (sortBy, sortDir?.ToLowerInvariant()) switch {
            ("updatedAt","desc") => items.OrderByDescending(x=>x.UpdatedAt).ToList(),
            ("updatedAt","asc") => items.OrderBy(x=>x.UpdatedAt).ToList(),
            ("name","desc") => items.OrderByDescending(x=>x.Name).ToList(),
            _ => items.OrderBy(x=>x.Name).ToList()
        };
        var total = items.Count;
        var pageItems = items.Skip((page-1)*pageSize).Take(pageSize).ToList();
        return Ok(new { total, page, pageSize, items = pageItems.Select(t => new { t.Id, t.Name, t.InputJson, t.ExpectJson, t.UpdatedAt, t.Suite, Tags = (t.TagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), t.Priority }).ToList() });
    }).ToList();
    }

    using DocflowRules.Api.Validation;
    public record UpsertTest(string Name, JsonObject Input, JsonObject Expect, string? Suite, string[]? Tags, int? Priority);
    

    [HttpPost]
    public async Task<ActionResult<object>> Create2(Guid ruleId, [FromBody] TestUpsertPayload body, CancellationToken ct)
    {
        return await Create(ruleId, new UpsertTest(body.Name, body.Input, body.Expect, body.Suite, body.Tags, body.Priority), ct);
    }

    
    public async Task<ActionResult<object>> Create(Guid ruleId, [FromBody] UpsertTest req, CancellationToken ct)
    {
        var r = await _repoRules.GetAsync(ruleId, ct);
        if (r == null) return NotFound();
        var t = new RuleTestCase { RuleFunctionId = ruleId, Name = req.Name, InputJson = req.Input.ToJsonString(), ExpectJson = req.Expect.ToJsonString(), Suite = req.Suite, TagsCsv = req.Tags is null ? null : string.Join(',', req.Tags), Priority = req.Priority ?? 3 };
        await _repoTests.AddAsync(t, ct);
        return CreatedAtAction(nameof(Get), new { ruleId }, new { t.Id, t.Name, t.InputJson, t.ExpectJson, t.UpdatedAt });
    }

    [Authorize(Policy=\"editor\")]
    [HttpPost("run")]
    public async Task<ActionResult<IEnumerable<object>>> Run(Guid ruleId, [FromBody] RunAllRequest? opt, CancellationToken ct)
    {
        var rule = await _repoRules.GetAsync(ruleId, ct);
        if (rule == null) return NotFound();

        var tests = await _repoTests.GetByRuleAsync(ruleId, ct);
        var results = new List<object>();
        foreach (var t in tests)
        {
            var input = System.Text.Json.Nodes.JsonNode.Parse(t.InputJson) as JsonObject ?? new();
            var expect = System.Text.Json.Nodes.JsonNode.Parse(t.ExpectJson) as JsonObject ?? new();

            var started = DateTimeOffset.UtcNow;
            string[] logs = Array.Empty<string>();
            try
            {
                var run = await _engine.RunAsync(rule.Code, input, ct);
                logs = run.Logs;
                var (passed, diff) = Compare(run.After, expect);
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                results.Add(new { id = t.Id, name = t.Name, passed, durationMs = dur, diff, logs, actual = run.After });
                _testDurationMs.Record(dur);
                if (passed) _testPass.Add(1); else _testFail.Add(1);
            }
            catch (Exception ex)
            {
                var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                results.Add(new { id = t.Id, name = t.Name, passed = false, durationMs = dur, error = ex.Message, logs, actual = (System.Text.Json.Nodes.JsonObject?)null });
            }
        }
        return Ok(results);
    }

    
    public record RunSelectedRequest(List<Guid> Ids, int? MaxParallelism);


    public record UpdateMeta(string? Name, string? Suite, string[]? Tags, int? Priority);

    [Authorize(Policy=\"editor\")]
    [HttpPut("{testId:guid}")]
    public async Task<ActionResult> Update(Guid ruleId, Guid testId, [FromBody] UpdateMeta req, CancellationToken ct)
    {
        var r = await _repoRules.GetAsync(ruleId, ct);
        if (r == null) return NotFound();
        var tests = await _repoTests.GetByRuleAsync(ruleId, ct);
        var t = tests.FirstOrDefault(x => x.Id == testId);
        if (t == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) t.Name = req.Name!;
        if (req.Suite is not null) t.Suite = req.Suite;
        if (req.Tags is not null) t.TagsCsv = string.Join(',', req.Tags);
        if (req.Priority.HasValue) t.Priority = req.Priority.Value;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _repoTests.UpdateAsync(t, ct);
        return NoContent();
    }


    public record RunAllRequest(int? MaxParallelism);

    [Authorize(Policy=\"editor\")]
    [HttpPost("run-selected")]
    public async Task<ActionResult<IEnumerable<object>>> RunSelected(Guid ruleId, [FromBody] RunSelectedRequest req, CancellationToken ct)
    {
        var rule = await _repoRules.GetAsync(ruleId, ct);
        if (rule == null) return NotFound();
        if (req?.Ids == null || req.Ids.Count == 0) return BadRequest("No test IDs provided");

        var tests = (await _repoTests.GetByRuleAsync(ruleId, ct)).Where(t => req.Ids.Contains(t.Id)).ToList();
        var results = new System.Collections.Concurrent.ConcurrentBag<object>();
        var maxPar = opt?.MaxParallelism ?? HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<int>("Testing:MaxParallelism");
        using var gate = new SemaphoreSlim(maxPar);
        var tasks = tests.Select(async t => {
            await gate.WaitAsync(ct);
            try {
                var input = System.Text.Json.Nodes.JsonNode.Parse(t.InputJson) as System.Text.Json.Nodes.JsonObject ?? new();
                var expect = System.Text.Json.Nodes.JsonNode.Parse(t.ExpectJson) as System.Text.Json.Nodes.JsonObject ?? new();
                var started = DateTimeOffset.UtcNow;
                string[] logs = Array.Empty<string>();
                try {
                    var run = await _engine.RunAsync(rule.Code, input, ct);
                    logs = run.Logs;
                    var (passed, diff) = Compare(run.After, expect);
                    var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                    results.Add(new { id = t.Id, name = t.Name, passed, durationMs = dur, diff, logs, actual = run.After, mutatedFields = run.Mutations.Select(m => (string)m! ["field"]).Where(f => f != null).Distinct().ToArray() });
                } catch (Exception ex) {
                    var dur = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                    results.Add(new { id = t.Id, name = t.Name, passed = false, durationMs = dur, error = ex.Message, logs, actual = (System.Text.Json.Nodes.JsonObject?)null, mutatedFields = Array.Empty<string>() });
                }
            } finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
        return Ok(results.ToArray());
    }


    [HttpGet("coverage")]
    public async Task<ActionResult<IEnumerable<object>>> Coverage(Guid ruleId, CancellationToken ct)
    {
        var rule = await _repoRules.GetAsync(ruleId, ct);
        if (rule == null) return NotFound();

        var tests = await _repoTests.GetByRuleAsync(ruleId, ct);
        var summary = new Dictionary<string, (int tested, int mutated, int hits, int pass)>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in tests)
        {
            var input = System.Text.Json.Nodes.JsonNode.Parse(t.InputJson) as System.Text.Json.Nodes.JsonObject ?? new();
            var expect = System.Text.Json.Nodes.JsonNode.Parse(t.ExpectJson) as System.Text.Json.Nodes.JsonObject ?? new();
            var expectFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (expect.TryGetPropertyValue("fields", out var fnode) && fnode is System.Text.Json.Nodes.JsonObject fields)
                foreach (var kv in fields) expectFields.Add(kv.Key);

            try
            {
                var run = await _engine.RunAsync(rule.Code, input, ct);
                var mutated = run.Mutations.Select(m => (string)m!["field"]).Where(f => f != null).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var failedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var (_, diffs) = Compare(run.After, expect);
                foreach (var d in diffs) { var f = (string?)d!["field"]; if (!string.IsNullOrWhiteSpace(f)) failedFields.Add(f!); }

                foreach (var field in expectFields.Union(mutated))
                {
                    var tested = expectFields.Contains(field) ? 1 : 0;
                    var mut = mutated.Contains(field) ? 1 : 0;
                    var hit = (tested == 1 && mut == 1) ? 1 : 0;
                    var pass = (tested == 1 && !failedFields.Contains(field)) ? 1 : 0;

                    if (!summary.ContainsKey(field)) summary[field] = (0,0,0,0);
                    var s = summary[field];
                    summary[field] = (s.tested + tested, s.mutated + mut, s.hits + hit, s.pass + pass);
                }
            }
            catch {}
        }

        var res = summary.Select(kv => new { field = kv.Key, tested = kv.Value.tested, mutated = kv.Value.mutated, hits = kv.Value.hits, pass = kv.Value.pass })
                         .OrderByDescending(x => x.hits).ThenBy(x => x.field).ToList();
        return Ok(res);
    }


    
    private static (bool ok, string? err) CompareField(System.Text.Json.Nodes.JsonNode? actual, System.Text.Json.Nodes.JsonObject rules)
    {
        bool hasRule = false;
        // exists
        if (rules.TryGetPropertyValue("exists", out var existsNode))
        {
            hasRule = true;
            bool exp = existsNode!.GetValue<bool>();
            bool exists = actual != null && !actual.ToJsonString().Equals("null", StringComparison.OrdinalIgnoreCase);
            if (exp != exists) return (false, $"exists expected {exp} but was {exists}");
        }
        // regex
        if (rules.TryGetPropertyValue("regex", out var regexNode))
        {
            hasRule = true;
            var pattern = regexNode!.ToString();
            var str = actual?.ToString() ?? string.Empty;
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(str, pattern)) return (false, $"regex '{pattern}' not match '{str}'");
            }
            catch (Exception ex) { return (false, $"invalid regex: {ex.Message}"); }
        }
        // approx
        if (rules.TryGetPropertyValue("approx", out var approxNode))
        {
            hasRule = true;
            double? value = null; double tol = 0;
            if (approxNode is System.Text.Json.Nodes.JsonValue aval && aval.TryGetValue<double>(out var d)) { value = d; }
            else if (approxNode is System.Text.Json.Nodes.JsonObject aobj)
            {
                if (aobj.TryGetPropertyValue("value", out var v2) && v2 is System.Text.Json.Nodes.JsonValue jv2 && jv2.TryGetValue<double>(out var d2)) value = d2;
                if (aobj.TryGetPropertyValue("tol", out var t2) && t2 is System.Text.Json.Nodes.JsonValue jv3 && jv3.TryGetValue<double>(out var d3)) tol = d3;
            }
            if (rules.TryGetPropertyValue("tol", out var tolNode) && tolNode is System.Text.Json.Nodes.JsonValue tv && tv.TryGetValue<double>(out var td)) tol = td;
            if (value is null) return (false, "approx missing numeric value");
            if (actual is null) return (false, "actual is null");
            if (!double.TryParse(actual.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var av)) return (false, "actual not numeric");
            if (Math.Abs(av - value.Value) > tol) return (false, $"|{av} - {value.Value}| > {tol}");
        }
        // equals
        if (rules.TryGetPropertyValue("equals", out var eqNode))
        {
            hasRule = true;
            var eqJson = eqNode!.ToJsonString(); var actJson = actual?.ToJsonString() ?? "null";
            if (eqJson != actJson) return (false, $"equals mismatch");
        }
        if (!hasRule) return (false, "no rule");
        return (true, null);
    }

    private static (bool passed, System.Text.Json.Nodes.JsonArray diff) Compare(System.Text.Json.Nodes.JsonNode actual, System.Text.Json.Nodes.JsonObject expect)
    {
        var diff = new System.Text.Json.Nodes.JsonArray();
        bool allOk = true;
        if (expect.TryGetPropertyValue("fields", out var fnode) && fnode is System.Text.Json.Nodes.JsonObject fields)
        {
            foreach (var kv in fields)
            {
                var name = kv.Key; var rules = kv.Value as System.Text.Json.Nodes.JsonObject ?? new();
                var a = (actual as System.Text.Json.Nodes.JsonObject)?[name];
                var (ok, err) = CompareField(a, rules);
                if (!ok)
                {
                    allOk = false;
                    var problem = new System.Text.Json.Nodes.JsonObject
                    {
                        ["field"] = name,
                        ["error"] = err ?? "mismatch",
                        ["expected"] = rules,
                        ["actual"] = a
                    };
                    diff.Add(problem);
                }
            }
        }
        return (allOk, diff);
    }
);
                }
                if (checks.TryGetPropertyValue("approx", out var approx))
                {
                    double tol = 0.01;
                    if (checks.TryGetPropertyValue("tol", out var t)) tol = (double)t!;
                    var av = aval is null ? (double?)null : (double?)Convert.ToDouble(aval!.ToString());
                    var exv = Convert.ToDouble(approx!.ToString());
                    var ok = av.HasValue && Math.Abs(av.Value - exv) <= tol;
                    allOk &= ok;
                    if (!ok) diffs.Add(new System.Text.Json.Nodes.JsonObject { ["field"] = field, ["rule"] = "approx", ["expected"] = approx, ["actual"] = aval, ["tol"] = tol });
                }
                if (checks.TryGetPropertyValue("exists", out var exists))
                {
                    bool ok = exists!.ToString().ToLowerInvariant() == "true" ? aval is not null : aval is null;
                    allOk &= ok;
                    if (!ok) diffs.Add(new System.Text.Json.Nodes.JsonObject { ["field"] = field, ["rule"] = "exists", ["expected"] = exists, ["actual"] = aval });
                }
                if (checks.TryGetPropertyValue("regex", out var regex))
                {
                    var avs = aval?.ToString() ?? "";
                    bool ok = System.Text.RegularExpressions.Regex.IsMatch(avs, regex!.ToString());
                    allOk &= ok;
                    if (!ok) diffs.Add(new System.Text.Json.Nodes.JsonObject { ["field"] = field, ["rule"] = "regex", ["expected"] = regex, ["actual"] = aval });
                }
            }
        }
        return (allOk, diffs);
    }

    public record CloneReq(string? NewName, string? Suite, string[]? Tags);

    [HttpPost("{testId:guid}/clone")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> CloneTest(Guid ruleId, Guid testId, [FromBody] CloneReq req, CancellationToken ct)
    {
        var tests = await _repoTests.GetByRuleAsync(ruleId, ct);
        var src = tests.FirstOrDefault(t => t.Id == testId);
        if (src == null) return NotFound();
        var t = new DocflowRules.Storage.EF.RuleTestCase {
            RuleFunctionId = ruleId,
            Name = string.IsNullOrWhiteSpace(req.NewName) ? src.Name + " (copy)" : req.NewName!,
            InputJson = src.InputJson,
            ExpectJson = src.ExpectJson,
            Suite = string.IsNullOrWhiteSpace(req.Suite) ? src.Suite : req.Suite,
            TagsCsv = (req.Tags?.Length ?? 0) > 0 ? string.Join(",", req.Tags!) : src.TagsCsv,
            Priority = src.Priority,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        t = await _repoTests.AddAsync(t, ct);
        return Ok(new { id = t.Id, name = t.Name });
    }

}
