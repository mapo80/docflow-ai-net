namespace DocflowRules.Storage.EF;

public enum RuleStatus { Draft, Staged, Published }

public class RuleFunction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Version { get; set; } = "1.0.0";
    public bool IsBuiltin { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
    public string Code { get; set; } = default!;
    public string CodeHash { get; set; } = default!;
    public string? ReadsCsv { get; set; }
    public string? WritesCsv { get; set; }
    public string? Owner { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public RuleStatus Status { get; set; } = RuleStatus.Draft;
    public string? SemVersion { get; set; } = "0.1.0";
    public string? Signature { get; set; }
    public bool BuiltinLocked { get; set; } = false;
}
