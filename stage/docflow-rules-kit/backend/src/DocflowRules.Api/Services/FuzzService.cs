using System.Text.Json.Nodes;
using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public interface IFuzzService
{
    Task<JsonArray> GenerateAsync(Guid ruleId, int maxPerField, CancellationToken ct);
    Task<int> ImportAsync(Guid ruleId, JsonArray payload, string? suite, string[]? tags, CancellationToken ct);
}

public class FuzzService : IFuzzService
{
    private readonly AppDbContext _db;
    public FuzzService(AppDbContext db) { _db = db; }

    public async Task<JsonArray> GenerateAsync(Guid ruleId, int maxPerField, CancellationToken ct)
    {
        var r = await _db.RuleFunctions.FirstOrDefaultAsync(x => x.Id == ruleId, ct);
        if (r == null) return new JsonArray();

        var sa = new StaticAnalyzer();
        var (fields, skeletons) = sa.Analyze(r.Code);

        var arr = new JsonArray();
        foreach (var f in fields.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            // simple numeric & exists fuzz
            var name = f.ToString();
            var nums = new[] { -1, 0, 1, 9, 10, 99, 100, 101, 1000 };
            int c = 0;
            foreach (var n in nums)
            {
                if (c >= maxPerField) break;
                var jo = new JsonObject {
                    ["name"] = $"{name} fuzz {n}",
                    ["suite"] = "fuzz",
                    ["tags"] = new JsonArray("fuzz"),
                    ["input"] = new JsonObject { [name] = n },
                    ["expect"] = new JsonObject { ["fields"] = new JsonObject() }
                };
                arr.Add(jo); c++;
            }
            if (c < maxPerField)
            {
                arr.Add(new JsonObject {
                    ["name"] = $"{name} missing",
                    ["suite"] = "fuzz",
                    ["tags"] = new JsonArray("fuzz","nullability"),
                    ["input"] = new JsonObject { }, // missing field
                    ["expect"] = new JsonObject { ["fields"] = new JsonObject() }
                });
            }
        }
        return arr;
    }

    public async Task<int> ImportAsync(Guid ruleId, JsonArray payload, string? suite, string[]? tags, CancellationToken ct)
    {
        int count = 0;
        foreach (var item in payload)
        {
            var jo = item!.AsObject();
            var t = new RuleTestCase {
                RuleFunctionId = ruleId,
                Name = jo["name"]?.ToString() ?? $"fuzz-{Guid.NewGuid():N}",
                InputJson = (jo["input"] ?? new JsonObject()).ToJsonString(),
                ExpectJson = (jo["expect"] ?? new JsonObject()).ToJsonString(),
                Suite = suite ?? jo["suite"]?.ToString() ?? "fuzz",
                TagsCsv = string.Join(",", (tags?.Length>0 ? tags : jo["tags"]?.AsArray()?.Select(x=>x!.ToString()).ToArray() ?? new [] {"fuzz"})!),
                Priority = 3,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.RuleTestCases.Add(t); count++;
        }
        await _db.SaveChangesAsync(ct);
        return count;
    }
}
