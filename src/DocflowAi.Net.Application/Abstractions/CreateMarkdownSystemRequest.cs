namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Request to create a new markdown system.</summary>
public class CreateMarkdownSystemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // docling | azure-di
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
}
