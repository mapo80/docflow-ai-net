using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public class AdminSeedHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AdminSeedHostedService> _log;
    public AdminSeedHostedService(IServiceProvider sp, IConfiguration cfg, ILogger<AdminSeedHostedService> log)
    { _sp = sp; _cfg = cfg; _log = log; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocflowRules.Storage.EF.AppDbContext>();

        var username = _cfg["Seed:Admin:Username"] ?? "admin";
        var email = _cfg["Seed:Admin:Email"] ?? "admin@example.com";
        var rolesCsv = _cfg["Seed:Admin:Roles"] ?? "admin,editor,reviewer,viewer";
        var roles = rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user == null)
        {
            user = new DocflowRules.Storage.EF.AppUser { Username = username, Email = email };
            db.Users.Add(user);
            _log.LogInformation("Seed: created admin user {Username}", username);
        }
        await db.SaveChangesAsync(cancellationToken);

        // Ensure roles
        foreach (var r in roles)
        {
            var exists = await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.Role == r, cancellationToken);
            if (!exists)
            {
                db.UserRoles.Add(new DocflowRules.Storage.EF.AppUserRole { UserId = user.Id, Role = r });
                _log.LogInformation("Seed: added role {Role} to {Username}", r, username);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
