using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Route("api/identity")]
[Authorize(Policy="admin")]
public class IdentityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<IdentityController> _log;
    public IdentityController(AppDbContext db, ILogger<IdentityController> log) { _db = db; _log = log; }

    [HttpGet("roles")]
    [AllowAnonymous]
    public ActionResult<IEnumerable<string>> Roles() => new[] { "viewer", "editor", "reviewer", "admin" };

    [HttpGet("users")]
    public async Task<ActionResult<object>> Users([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(u => u.Username.Contains(search) || (u.Email ?? "").Contains(search));
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(u=>u.Username).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        var withRoles = new List<object>();
        foreach (var u in items)
        {
            var roles = await _db.UserRoles.Where(r => r.UserId == u.Id).Select(r => r.Role).ToListAsync(ct);
            withRoles.Add(new { u.Id, u.Username, u.Email, u.CreatedAt, Roles = roles });
        }
        return Ok(new { total, page, pageSize, items = withRoles });
    }

    public record UpsertUser(string Username, string? Email, string[]? Roles);

    [HttpPost("users")]
    public async Task<ActionResult> CreateUser([FromBody] UpsertUser req, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username, ct)) return Conflict("Username already exists");
        var u = new AppUser { Username = req.Username, Email = req.Email };
        await _db.Users.AddAsync(u, ct);
        if (req.Roles != null) foreach (var r in req.Roles.Distinct()) _db.UserRoles.Add(new AppUserRole { UserId = u.Id, Role = r });
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("User {Username} created", req.Username);
        return NoContent();
    }

    [HttpPut("users/{id:guid}")]
    public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UpsertUser req, CancellationToken ct)
    {
        var u = await _db.Users.FindAsync(new object?[]{id}, ct);
        if (u == null) return NotFound();
        u.Email = req.Email; u.Username = req.Username;
        var current = _db.UserRoles.Where(x => x.UserId == id);
        _db.UserRoles.RemoveRange(current);
        if (req.Roles != null) foreach (var r in req.Roles.Distinct()) _db.UserRoles.Add(new AppUserRole { UserId = id, Role = r });
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("User {Id} updated", id);
        return NoContent();
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var u = await _db.Users.FindAsync(new object?[]{id}, ct);
        if (u == null) return NotFound();
        _db.Users.Remove(u);
        _db.UserRoles.RemoveRange(_db.UserRoles.Where(r => r.UserId == id));
        await _db.SaveChangesAsync(ct);
        _log.LogWarning("User {Id} deleted", id);
        return NoContent();
    }
}
