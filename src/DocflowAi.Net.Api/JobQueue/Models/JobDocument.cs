namespace DocflowAi.Net.Api.JobQueue.Models;

public class JobDocument
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Queued";
    public int Progress { get; set; }
    public int Attempts { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MetricsInfo Metrics { get; set; } = new();
    public string? IdempotencyKey { get; set; }
    public string Hash { get; set; } = string.Empty;
    public PathInfo Paths { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string Model { get; set; } = string.Empty;
    public string TemplateToken { get; set; } = string.Empty;

    public class MetricsInfo
    {
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public long? DurationMs { get; set; }
    }

    public class PathInfo
    {
        public string Dir { get; set; } = string.Empty;
        public string? Input { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public string? Markdown { get; set; }
    }
}
