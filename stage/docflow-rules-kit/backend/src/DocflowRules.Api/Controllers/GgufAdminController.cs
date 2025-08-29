using DocflowRules.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Route("api/admin/gguf")]
[Authorize(Policy="admin")]
public class GgufAdminController : ControllerBase
{
    private readonly IGgufService _svc;
    public GgufAdminController(IGgufService svc) { _svc = svc; }

    public record DownloadReq(string Repo, string File, string? Revision);

    [HttpPost("download")]
    public async Task<ActionResult<object>> Download([FromBody] DownloadReq req, CancellationToken ct)
    {
        var job = await _svc.EnqueueDownloadAsync(req.Repo, req.File, req.Revision, ct);
        return Accepted(new { jobId = job.Id });
    }

    [HttpGet("jobs/{id}")]
    public async Task<ActionResult<object>> Job(Guid id, CancellationToken ct)
    {
        var j = await _svc.GetJobAsync(id, ct);
        if (j == null) return NotFound();
        return Ok(new { j.Id, j.Status, j.Progress, j.FilePath, j.Error, j.CreatedAt, j.UpdatedAt });
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<object>> Jobs(CancellationToken ct)
        => Ok(await _svc.ListJobsAsync(ct));

    [HttpGet("available")]
    public async Task<ActionResult<object>> Available(CancellationToken ct)
        => Ok(await _svc.ListAvailableAsync(ct));
}

    public record DeleteReq(string Path);

    [HttpDelete("available")]
    public async Task<ActionResult> Delete([FromBody] DeleteReq req, CancellationToken ct)
    {
        try
        {
            var ok = await _svc.DeleteAvailableAsync(req.Path, ct);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
