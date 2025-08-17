using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using DocflowRules.Storage.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DocflowRules.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthController> _log;

    public AuthController(AppDbContext db, IConfiguration cfg, ILogger<AuthController> log)
    { _db = db; _cfg = cfg; _log = log; }

    public record LoginReq(string Username, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Login([FromBody] LoginReq req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username, ct);
        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "invalid_credentials" });

        var roles = await _db.UserRoles.Where(r => r.UserId == user.Id).Select(r => r.Role).ToListAsync(ct);

        var issuer = _cfg["Auth:Local:Issuer"] ?? "docflow-local";
        var audience = _cfg["Auth:Local:Audience"] ?? "docflow-ui";
        var key = _cfg["Auth:Local:SigningKey"] ?? "dev-signing-key-change-me-please-please";
        var lifetime = int.TryParse(_cfg["Auth:Local:TokenLifetimeMinutes"], out var mins) ? mins : 120;

        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.PreferredUsername, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.Username)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(issuer, audience, claims, now, now.AddMinutes(lifetime), creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { access_token = jwt, token_type = "Bearer", expires_in = lifetime * 60 });
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var name = User.Identity?.Name ?? "";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(new { name, roles });
    }
}
