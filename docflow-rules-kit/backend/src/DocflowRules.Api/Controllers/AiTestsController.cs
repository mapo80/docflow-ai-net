using DocflowRules.Api.Services;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Route("api/ai/tests")]
[Authorize(Policy="editor")]
public class AiTestsController : ControllerBase
{
    private readonly SuggestionService _svc;
    private readonly AppDbContext _db;
    private readonly ILogger<AiTestsController> _log;

    public AiTestsController(SuggestionService svc, AppDbContext db, ILogger<AiTestsController> log)
    { _svc = svc; _db = db; _log = log; }

    public record SuggestReq(string? UserPrompt, int? Budget, double? Temperature, Guid? ModelId, bool? Turbo);
    public record SuggestRes(IEnumerable<object> Suggestions, string Model, int TotalSkeletons, int InputTokens, int OutputTokens, long DurationMs, double CostUsd);

    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestRes>> Suggest([FromQuery] Guid ruleId, [FromBody] SuggestReq req, CancellationToken ct)
    {
        var (suggs, model, total, inTok, outTok, durMs, cost) = await _svc.SuggestAsync(ruleId, req.UserPrompt, req.Budget ?? 20, req.Temperature ?? 0.2, req.ModelId, req.Turbo, ct);
        var shaped = suggs.Select(s => new {
            id = s.Id,
            reason = s.Reason,
            score = s.Score,
            coverageDelta = JsonSerializer.Deserialize<object>(s.CoverageDeltaJson),
            payload = JsonSerializer.Deserialize<object>(s.PayloadJson),
            createdAt = s.CreatedAt,
            model = s.Model
        }).ToList();
        return Ok(new SuggestRes(shaped, model, total, inTok, outTok, durMs, cost));
    }

    public record ImportReq(Guid[] Ids, string? Suite, string[]? Tags);

    [HttpPost("import")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> Import([FromQuery] Guid ruleId, [FromBody] ImportReq req, CancellationToken ct)
    {
        var count = await _svc.ImportAsync(ruleId, req.Ids, req.Suite, req.Tags, ct);
        _log.LogInformation("Imported {Count} AI suggestions into tests for rule {RuleId}", count, ruleId);
        return Ok(new { imported = count });
    }
}
