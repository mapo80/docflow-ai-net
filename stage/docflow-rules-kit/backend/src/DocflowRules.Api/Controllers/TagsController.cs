using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly ITestTagRepository _repo;
    private readonly ILogger<TagsController> _log;
    public TagsController(ITestTagRepository repo, ILogger<TagsController> log) { _repo = repo; _log = log; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestTag>>> GetAll(CancellationToken ct) => await _repo.GetAllAsync(ct);

    using DocflowRules.Api.Validation;
    public record Upsert(string Name, string? Color, string? Description);
    

    [HttpPost]
    public async Task<ActionResult<TestTag>> Create([FromBody] Upsert req, CancellationToken ct)
    {
        if (await _repo.GetByNameAsync(req.Name, ct) != null) return Conflict("Tag already exists");
        var t = await _repo.AddAsync(new TestTag { Name = req.Name, Color = req.Color, Description = req.Description }, ct);
        _log.LogInformation("Tag created {Name}", t.Name);
        return CreatedAtAction(nameof(GetAll), new { id = t.Id }, t);
    }

    [Authorize(Policy=\"editor\")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Upsert req, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t == null) return NotFound();
        t.Name = req.Name; t.Color = req.Color; t.Description = req.Description; t.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(t, ct);
        _log.LogInformation("Tag updated {Id}", id);
        return NoContent();
    }

    [Authorize(Policy=\"editor\")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) { await _repo.DeleteAsync(id, ct); _log.LogWarning("Tag deleted {Id}", id); return NoContent(); }
}
