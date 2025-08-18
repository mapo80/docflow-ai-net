namespace DocflowAi.Net.Api.Templates.Models;

public class TemplateDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? PromptMarkdown { get; set; }
    public string FieldsJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
