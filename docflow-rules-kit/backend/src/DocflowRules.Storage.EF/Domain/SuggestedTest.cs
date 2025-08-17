namespace DocflowRules.Storage.EF;

public class SuggestedTest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RuleId { get; set; }
    public string PayloadJson { get; set; } = default!; // serialized TestUpsertPayload
    public string Reason { get; set; } = "";
    public string CoverageDeltaJson { get; set; } = "[]"; // [{field, delta}]
    public double Score { get; set; }
    public string Hash { get; set; } = ""; // hash(ruleCode + payload)
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Model { get; set; } = "static-v1";
}
