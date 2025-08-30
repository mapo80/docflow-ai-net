namespace DocflowAi.Net.Api.Rules.Models;

public class SuggestedTest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RuleId { get; set; }
    public string PayloadJson { get; set; } = default!;
    public string Reason { get; set; } = "";
    public string CoverageDeltaJson { get; set; } = "[]";
    public double Score { get; set; }
    public string Hash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Model { get; set; } = "static-v1";
}
