namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Request to update an existing markdown system.</summary>
public class UpdateMarkdownSystemRequest
{
    public required string Name { get; init; }
    public required string Provider { get; init; }
    public required string Endpoint { get; init; }
    public string? ApiKey { get; init; }
}
