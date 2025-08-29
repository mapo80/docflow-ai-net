namespace DocflowRules.Storage.EF;

public class GgufDownloadJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Repo { get; set; } = default!;      // e.g. DavidAU/Qwen3-...
    public string File { get; set; } = default!;      // e.g. model.q4_k_m.gguf
    public string Revision { get; set; } = "main";    // e.g. main
    public string Status { get; set; } = "queued";    // queued|running|succeeded|failed
    public int Progress { get; set; } = 0;            // 0..100
    public string? FilePath { get; set; }             // absolute path saved
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
