namespace DocflowAi.Net.Api.Model.Models;

/// <summary>
/// Entity representing a model configuration.
/// </summary>
public class ModelDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // hosted-llm | local
    public string? Provider { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKeyEncrypted { get; set; }
    public string? HfRepo { get; set; }
    public string? ModelFile { get; set; }
    public string? HfTokenEncrypted { get; set; }
    public string? DownloadStatus { get; set; }
    public bool? Downloaded { get; set; }
    public DateTimeOffset? DownloadedAt { get; set; }
    public string? LocalPath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Checksum { get; set; }
    public string? DownloadLogPath { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
