namespace DocflowRules.Storage.EF;

public class RuleTestCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RuleFunctionId { get; set; }
    public string Name { get; set; } = default!;
    public string InputJson { get; set; } = default!;
    public string ExpectJson { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Suite { get; set; }
    public string? TagsCsv { get; set; }
    public int Priority { get; set; } = 3;
}
