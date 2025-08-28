namespace DocflowAi.Net.Application.Abstractions;

/// <summary>DTO representing a markdown conversion system.</summary>
public record MarkdownSystemDto(
    Guid Id,
    string Name,
    string Provider,
    string Endpoint,
    bool HasApiKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
