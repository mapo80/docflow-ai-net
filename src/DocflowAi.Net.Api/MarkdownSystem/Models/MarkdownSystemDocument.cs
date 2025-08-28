namespace DocflowAi.Net.Api.MarkdownSystem.Models;

/// <summary>Entity representing a markdown conversion system.</summary>
public class MarkdownSystemDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // docling | azure-di
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKeyEncrypted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
