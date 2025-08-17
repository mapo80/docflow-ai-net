using DocflowRules.Api.Services;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Route("api/admin/llm")]
[Authorize(Policy="admin")]
public class LlmAdminController : ControllerBase
{
    private readonly ILlmConfigService _svc;
    private readonly ILLMProviderRegistry _reg;

    public LlmAdminController(ILlmConfigService svc, ILLMProviderRegistry reg)
    { _svc = svc; _reg = reg; }

    [HttpGet("models")]
    public async Task<ActionResult<IEnumerable<LlmModel>>> List(CancellationToken ct)
        => Ok(await _svc.ListAsync(ct));

    [HttpPost("models")]
    public async Task<ActionResult<LlmModel>> Create([FromBody] LlmModel m, CancellationToken ct)
        => Ok(await _svc.CreateAsync(m, ct));

    [HttpPut("models/{id}")]
    public async Task<ActionResult<LlmModel>> Update(Guid id, [FromBody] LlmModel m, CancellationToken ct)
    { m.Id = id; return Ok(await _svc.UpdateAsync(m, ct)); }

    [HttpDelete("models/{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    { await _svc.DeleteAsync(id, ct); return NoContent(); }

    public record ActivateReq(Guid ModelId, bool Turbo);

    [HttpPost("activate")]
    public async Task<ActionResult> Activate([FromBody] ActivateReq req, CancellationToken ct)
    { await _svc.SetActiveAsync(req.ModelId, req.Turbo, ct); return Ok(); }

    public record WarmupReq(Guid? ModelId);

    [HttpPost("warmup")]
    public async Task<ActionResult> Warmup([FromBody] WarmupReq req, CancellationToken ct)
    {
        var model = req.ModelId.HasValue ? (await _svc.GetByIdAsync(req.ModelId.Value, ct)) : (await _svc.GetActiveAsync(ct));
        if (model == null) return NotFound();
        var prov = _reg.GetProvider(model.Provider);
        if (prov is ILLMProviderConfigurable conf)
        {
            conf.SetRuntimeConfig(model, false);
            await conf.WarmupAsync(ct);
        }
        return Ok();
    }
}
