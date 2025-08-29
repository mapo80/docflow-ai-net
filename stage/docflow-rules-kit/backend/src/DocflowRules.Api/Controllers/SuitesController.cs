using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/suites")]
public class SuitesController : ControllerBase
{
    private readonly ITestSuiteRepository _repo;
    private readonly ILogger<SuitesController> _log;

    public SuitesController(ITestSuiteRepository repo, ILogger<SuitesController> log) { _repo = repo; _log = log; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestSuite>>> GetAll(CancellationToken ct) => await _repo.GetAllAsync(ct);

    using DocflowRules.Api.Validation;
    public record Upsert(string Name, string? Color, string? Description);
    

    [HttpPost]
    public async Task<ActionResult<TestSuite>> Create([FromBody] Upsert req, CancellationToken ct)
    {
        if (await _repo.GetByNameAsync(req.Name, ct) != null) return Conflict("Suite already exists");
        var s = await _repo.AddAsync(new TestSuite { Name = req.Name, Color = req.Color, Description = req.Description }, ct);
        _log.LogInformation("Suite created {Name}", s.Name);
        return CreatedAtAction(nameof(GetAll), new { id = s.Id }, s);
    }

    [Authorize(Policy=\"editor\")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Upsert req, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(id, ct);
        if (s == null) return NotFound();
        s.Name = req.Name; s.Color = req.Color; s.Description = req.Description; s.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(s, ct);
        _log.LogInformation("Suite updated {Id}", id);
        return NoContent();
    }

    [Authorize(Policy=\"editor\")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) { await _repo.DeleteAsync(id, ct); _log.LogWarning("Suite deleted {Id}", id); return NoContent(); }

    public record CloneReq(string NewName);

    [Authorize(Policy="editor")]
    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<object>> Clone(Guid id, [FromBody] CloneReq req, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(id, ct);
        if (s == null) return NotFound();
        var exists = await _repo.GetByNameAsync(req.NewName, ct);
        if (exists != null) return Conflict("Suite with this name already exists");
        var dup = await _repo.AddAsync(new TestSuite { Name = req.NewName, Color = s.Color, Description = s.Description }, ct);
        return Ok(new { id = dup.Id, name = dup.Name });
    }

}
