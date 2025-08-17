using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public static class SeedData
{
    public static async Task RunAsync(AppDbContext db, IConfiguration cfg, ILogger logger, CancellationToken ct = default)
    {
        // Admin user seed
        var enabled = bool.TryParse(cfg["Seed:Admin:Enabled"], out var e) ? e : true;
        if (!enabled) return;

        var username = cfg["Seed:Admin:Username"] ?? "admin";
        var email = cfg["Seed:Admin:Email"] ?? "admin@local";
        var password = cfg["Seed:Admin:Password"] ?? "changeme!";

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user == null)
        {
            user = new AppUser { Username = username, Email = email };
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await db.Users.AddAsync(user, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seed: created admin user '{Username}'", username);
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash)) { user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password); }
        var hasAdmin = await db.UserRoles.AnyAsync(r => r.UserId == user.Id && r.Role == "admin", ct);
        if (!hasAdmin)
        {
            db.UserRoles.Add(new AppUserRole { UserId = user.Id, Role = "admin" });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seed: assigned 'admin' role to '{Username}'", username);
        }
    }
}
