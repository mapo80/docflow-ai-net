namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Request to create a new model entry.
/// </summary>
public class CreateModelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "hosted-llm" or "local"
    public string? Provider { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? HfRepo { get; set; }
    public string? ModelFile { get; set; }
    public string? HfToken { get; set; }
}
