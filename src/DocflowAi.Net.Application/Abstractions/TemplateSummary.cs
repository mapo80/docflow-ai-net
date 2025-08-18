namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Summary information for template listings.
/// </summary>
public record TemplateSummary(
    Guid Id,
    string Name,
    string Token,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
