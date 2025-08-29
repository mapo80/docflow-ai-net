using System.Text.Json.Nodes;
using DocflowRules.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rules/{ruleId:guid}/fuzz")]
public class FuzzController : ControllerBase
{
    private readonly IFuzzService _svc;
    public FuzzController(IFuzzService svc) { _svc = svc; }

    [HttpPost("preview")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> Preview(Guid ruleId, [FromQuery] int maxPerField = 5, CancellationToken ct = default)
    {
        var arr = await _svc.GenerateAsync(ruleId, maxPerField, ct);
        return Ok(new { items = arr });
    }

    public record ImportReq(JsonArray Items, string? Suite, string[]? Tags);

    [HttpPost("import")]
    [Authorize(Policy="editor")]
    public async Task<ActionResult<object>> Import(Guid ruleId, [FromBody] ImportReq req, CancellationToken ct = default)
    {
        var n = await _svc.ImportAsync(ruleId, req.Items, req.Suite, req.Tags, ct);
        return Ok(new { imported = n });
    }
}
