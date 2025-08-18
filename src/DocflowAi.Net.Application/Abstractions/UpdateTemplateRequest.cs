namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Request payload for updating a template.
/// </summary>
public record UpdateTemplateRequest(
    string? Name,
    string? Token,
    string? PromptMarkdown,
    System.Text.Json.JsonElement? FieldsJson);
