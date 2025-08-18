namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Request payload for creating a template.
/// </summary>
public record CreateTemplateRequest(
    string Name,
    string Token,
    string? PromptMarkdown,
    System.Text.Json.JsonElement FieldsJson);
