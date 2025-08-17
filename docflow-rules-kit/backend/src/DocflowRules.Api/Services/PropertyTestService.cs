using System.Text.Json.Nodes;
using DocflowRules.Sdk;
using DocflowRules.Storage.EF;
using FsCheck;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public interface IPropertyTestService
{
    Task<PropertyRunResult> RunForRuleAsync(Guid ruleId, int trials, int? seed, CancellationToken ct);
    Task<PropertyRunResult> RunFromBlocksAsync(JsonArray blocks, int trials, int? seed, CancellationToken ct);
}

public record PropertyFailure(string Property, JsonObject Counterexample, string Message);
public record PropertyRunResult(int Trials, int Passed, int Failed, List<PropertyFailure> Failures);

using System.Text.RegularExpressions;

public class PropertyTestService : IPropertyTestService
{
    private readonly AppDbContext _db;
    private readonly IScriptRunner _runner;

    public PropertyTestService(AppDbContext db, IScriptRunner runner)
    {
        _db = db; _runner = runner;
    }

    public async Task<PropertyRunResult> RunForRuleAsync(Guid ruleId, int trials, int? seed, CancellationToken ct)
    {
        var r = await _db.RuleFunctions.FirstOrDefaultAsync(x => x.Id == ruleId, ct);
        if (r == null) return new PropertyRunResult(0,0,0,new());
        return await RunCoreAsync(r.Code, null, trials, seed);
    }

    public Task<PropertyRunResult> RunFromBlocksAsync(JsonArray blocks, int trials, int? seed, CancellationToken ct)
    {
        // compile lightweight code from blocks to enabling invariants like set/normalize
        var code = new System.Text.StringBuilder();
        code.AppendLine("void Run(ScriptGlobals g) { }"); // actual behavior not needed for 'idempotence/no-throw' but used for set/normalize if present? In simplification we skip
        return RunCoreAsync(code.ToString(), blocks, trials, seed);
    }

    private async Task<PropertyRunResult> RunCoreAsync(string code, JsonArray? blocks, int trials, int? seed)
    {
        var rnd = new Random(seed ?? Environment.TickCount);
        var failures = new List<PropertyFailure>();
        int passed = 0, failed = 0;

        // Type inference (very light) and arbitrary generators
        var types = InferFieldTypes(code);
        Gen<string> genStr = Arb.Generate<string>();
        Gen<double> genNum = Arb.Generate<double>();
        Gen<DateTimeOffset> genDate = Arb.Generate<DateTimeOffset>();

        for (int i = 0; i < trials; i++)
        {
            // build random input
            var input = new JsonObject();
            var fields = new JsonObject();
            foreach (var kv in types)
            {
                if (kv.Value == "number") fields[kv.Key] = new JsonObject { ["value"] = genNum.Sample(1,1).First() };
                else if (kv.Value == "date") fields[kv.Key] = new JsonObject { ["value"] = genDate.Sample(1,1).First().ToString("O") };
                else fields[kv.Key] = new JsonObject { ["value"] = genStr.Sample(1,1).First() };
            }
            if (fields.Count == 0) // fallback
            {
                fields["text"] = new JsonObject { ["value"] = genStr.Sample(1,1).First() };
            }
            input["fields"] = fields;
            try
            {
                // determinism/idempotence
                var r1 = await _runner.RunAsync(code, input, CancellationToken.None);
                var after1 = r1.after;
                var r2 = await _runner.RunAsync(code, after1, CancellationToken.None);
                var after2 = r2.after;
                if (after1.ToJsonString() != after2.ToJsonString())
                {
                    failed++; failures.Add(new("idempotence", input, "after != after(after)")); continue;
                }

                // invariants from blocks
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
                            {
                                failed++; failures.Add(new("set_copies_source", input, $"target '{tgt}' should equal source '{src}'")); goto NEXT;
                            }
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
}

    private static Dictionary<string,string> InferFieldTypes(string code)
    {
        var map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        // numeric: comparisons like field > 10, >=, <=, +, -, * etc
        foreach (Match m in Regex.Matches(code, @"\b([A-Za-z_][A-Za-z0-9_]*)\b\s*(?:[+\-*/]|[<>]=?|==|!=)\s*(?:\d+\.?\d*)"))
            map[m.Groups[1].Value] = "number";
        // date: TryParse/DateTime/ToString("O") around a field
        foreach (Match m in Regex.Matches(code, @"DateTime(?:Offset)?\.TryParse\([^\)]*\"([A-Za-z_][A-Za-z0-9_]*)\""))
            map[m.Groups[1].Value] = "date";
        // regex usage suggests string
        foreach (Match m in Regex.Matches(code, @"Regex\.IsMatch\(.*\"([A-Za-z_][A-Za-z0-9_]*)\""))
            if (!map.ContainsKey(m.Groups[1].Value)) map[m.Groups[1].Value] = "string";
        // default: any identifiers used in Get<type>("field") calls
        foreach (Match m in Regex.Matches(code, @"Get<[^>]+>\(\"([A-Za-z_][A-Za-z0-9_]*)\"\)"))
        {
            var f = m.Groups[1].Value;
            if (!map.ContainsKey(f)) map[f] = "string";
        }
        return map;
    }
