using System.Text.Json.Nodes;
using DocflowRules.Api.DTO;
using DocflowRules.Sdk;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ILogger<RulesController> _log;
    private readonly IRuleFunctionRepository _repo;
    private readonly IRuleTestCaseRepository _repoTests;
    private readonly IRuleEngine _engine;
    private readonly IScriptRunner _runner;

    public RulesController(ILogger<RulesController> log, IRuleFunctionRepository repo, IRuleEngine engine, IScriptRunner runner) { _repo = repo; _repoTests = repoTests; _engine = engine; _runner = runner; _log = log; }

    [HttpGet]
    [HttpGet]
    public async Task<ActionResult<object>> GetAll([FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        _log.LogInformation("GET /rules search={Search} sortBy={SortBy} sortDir={SortDir} page={Page} pageSize={PageSize}", search, sortBy, sortDir, page, pageSize);
        var items = await _repo.GetAllAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || (x.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        items = (sortBy, sortDir?.ToLowerInvariant()) switch {
            ("name","desc") => items.OrderByDescending(x=>x.Name).ToList(),
            ("updatedAt","asc") => items.OrderBy(x=>x.UpdatedAt).ToList(),
            ("updatedAt","desc") => items.OrderByDescending(x=>x.UpdatedAt).ToList(),
            _ => items.OrderBy(x=>x.Name).ToList()
        };
        var total = items.Count;
        var pageItems = items.Skip((page-1)*pageSize).Take(pageSize).ToList();
        return Ok(new { total, page, pageSize, items = pageItems.Select(x=> new RuleSummaryDto(x)).ToList() });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RuleDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        return new RuleDetailDto(r.Id, r.Name, r.Version, r.IsBuiltin, r.Enabled, r.Description, r.Code, r.ReadsCsv, r.WritesCsv, r.UpdatedAt);
    }

    public record UpsertRule(string Name, string? Description, string Code, string? ReadsCsv, string? WritesCsv, bool Enabled);

    [HttpPost]
    public async Task<ActionResult<RuleDetailDto>> Create([FromBody] UpsertRule req, CancellationToken ct)
    {
        if (await _repo.GetByNameAsync(req.Name, ct) != null) return Conflict("Name already exists");
        var rule = new RuleFunction {
            Name = req.Name, Description = req.Description, Code = req.Code,
            CodeHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Code))),
            ReadsCsv = req.ReadsCsv, WritesCsv = req.WritesCsv, IsBuiltin = false, Enabled = req.Enabled
        };
        await _repo.AddAsync(rule, ct);
        return CreatedAtAction(nameof(Get), new { id = rule.Id }, new RuleDetailDto(rule.Id, rule.Name, rule.Version, rule.IsBuiltin, rule.Enabled, rule.Description, rule.Code, rule.ReadsCsv, rule.WritesCsv, rule.UpdatedAt));
    }

    [Authorize(Policy=\"editor\")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpsertRule req, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        if (r.IsBuiltin) return BadRequest("Built-in rules are read-only");
        r.Name = req.Name; r.Description = req.Description; r.Code = req.Code;
        r.CodeHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Code)));
        r.ReadsCsv = req.ReadsCsv; r.WritesCsv = req.WritesCsv; r.Enabled = req.Enabled; r.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(r, ct);
        return NoContent();
    }

    [Authorize(Policy=\"editor\")]
    
    [HttpPost("{id:guid}/stage")]
    [Authorize(Policy="reviewer")]
    public async Task<ActionResult> Stage(Guid id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        r.Status = DocflowRules.Storage.EF.RuleStatus.Staged; r.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(r, ct); _log.LogInformation("Rule {Id} staged", id);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy="admin")]
    public async Task<ActionResult> Publish(Guid id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        r.Status = DocflowRules.Storage.EF.RuleStatus.Published; r.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(r, ct); _log.LogInformation("Rule {Id} published", id);
        return NoContent();
    }

    [HttpPost("{id:guid}/compile")]
    public async Task<ActionResult> Compile(Guid id, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        _log.LogInformation("POST /rules/{Id}/compile", id);
        var (ok, errors) = await _runner.CompileAsync(r.Code, ct);
        _log.LogInformation("Compile result: {Ok} errors: {Count}", ok, errors.Length);
        return Ok(new { ok, errors });
    }

    public record RunBody(JsonObject Input);

    [Authorize(Policy=\"editor\")]
    [HttpPost("{id:guid}/run")]
    public async Task<ActionResult<RunResultDto>> Run(Guid id, [FromBody] RunBody body, CancellationToken ct)
    {
        var r = await _repo.GetAsync(id, ct);
        if (r == null) return NotFound();
        _log.LogInformation("POST /rules/{Id}/run", id);
        var started = DateTimeOffset.UtcNow;
        var res = await _engine.RunAsync(r.Code, body.Input, ct);
        var ms = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
        _log.LogInformation("Run done in {Ms} ms; mutations {Mut}", ms, res.Mutations.Count);
        return Ok(new RunResultDto(res.Before, res.After, res.Mutations, res.DurationMs, res.Logs));
    }

    [Authorize(Policy="editor")]
    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<object>> Clone(Guid id, [FromBody] CloneReq req, CancellationToken ct)
    {
        var src = await _repo.GetAsync(id, ct);
        if (src == null) return NotFound();
        var name = string.IsNullOrWhiteSpace(req.NewName) ? src.Name + " (copy)" : req.NewName!;
        var dup = new DocflowRules.Storage.EF.RuleFunction {
            Name = name, Version = src.Version, IsBuiltin = false, Enabled = src.Enabled, Description = src.Description,
            Code = src.Code, CodeHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(src.Code)).AsSpan().ToString()
        };
        dup = await _repo.AddAsync(dup, ct);

        if (req.IncludeTests == true)
        {
            var tests = await _repoTests.GetByRuleAsync(id, ct);
            foreach (var t in tests)
            {
                await _repoTests.AddAsync(new DocflowRules.Storage.EF.RuleTestCase {
                    RuleFunctionId = dup.Id,
                    Name = t.Name, InputJson = t.InputJson, ExpectJson = t.ExpectJson, Suite = t.Suite, TagsCsv = t.TagsCsv, Priority = t.Priority,
                    UpdatedAt = DateTimeOffset.UtcNow
                }, ct);
            }
        }
        return Ok(new { id = dup.Id, name = dup.Name });
    }

    public record CloneReq(string? NewName, bool? IncludeTests);

}
