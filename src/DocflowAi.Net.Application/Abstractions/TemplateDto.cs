namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// DTO representing a template with fields definition.
/// </summary>
public record TemplateDto(
    Guid Id,
    string Name,
    string Token,
    string? PromptMarkdown,
    System.Text.Json.JsonElement FieldsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
