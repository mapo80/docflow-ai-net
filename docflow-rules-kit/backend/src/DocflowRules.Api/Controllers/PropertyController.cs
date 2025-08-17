using System.Text.Json.Nodes;
using DocflowRules.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rules")]
public class PropertyController : ControllerBase
{
    private readonly IPropertyTestService _svc;
    private readonly DocflowRules.Storage.EF.AppDbContext _svc_db;
    public PropertyController(IPropertyTestService svc, DocflowRules.Storage.EF.AppDbContext db) { _svc = svc; _svc_db = db; }

    [HttpPost("{ruleId:guid}/properties/run")]
    public async Task<ActionResult<object>> Run(Guid ruleId, [FromQuery] int trials = 100, [FromQuery] int? seed = null, CancellationToken ct = default)
    {
        var res = await _svc.RunForRuleAsync(ruleId, trials, seed, ct);
        return Ok(res);
    }

    public record BlocksReq(JsonArray Blocks, int Trials, int? Seed);

    [HttpPost("properties/runFromBlocks")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> RunFromBlocks([FromBody] BlocksReq req, CancellationToken ct = default)
    {
        var res = await _svc.RunFromBlocksAsync(req.Blocks, req.Trials, req.Seed, ct);
        return Ok(res);
    }
}


    public record ImportFailuresReq(PropertyFailure[] Failures, string? Suite, string[]? Tags);

    [HttpPost("{ruleId:guid}/properties/importFailures")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> ImportFailures(Guid ruleId, [FromBody] ImportFailuresReq req, CancellationToken ct = default)
    {
        // Map failures to test cases (input = counterexample)
        int count = 0;
        foreach (var f in req.Failures ?? Array.Empty<PropertyFailure>())
        {
            var t = new DocflowRules.Storage.EF.RuleTestCase {
                RuleFunctionId = ruleId,
                Name = $"propfail - {f.Property} - {DateTimeOffset.UtcNow:HHmmss}-{count+1}",
                InputJson = (f.Counterexample ?? new JsonObject()).ToJsonString(),
                ExpectJson = new JsonObject().ToJsonString(),
                Suite = req.Suite ?? "property-fails",
                TagsCsv = string.Join(",", (req.Tags?.Length>0 ? req.Tags : new [] { "property", f.Property })) ,
                Priority = 2,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _svc_db.RuleTestCases.Add(t); count++;
        }
        await _svc_db.SaveChangesAsync(ct);
        return Ok(new { imported = count });
    }
