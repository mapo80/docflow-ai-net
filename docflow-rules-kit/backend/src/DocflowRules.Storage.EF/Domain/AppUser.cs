namespace DocflowRules.Storage.EF;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = default!;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? PasswordHash { get; set; }
}

public class AppUserRole
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
}
