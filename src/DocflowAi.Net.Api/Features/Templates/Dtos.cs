using System.ComponentModel.DataAnnotations;

namespace DocflowAi.Net.Api.Features.Templates;

public record TemplateUpsertRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(100)] string? DocumentType,
    [property: MaxLength(20)] string? Language,
    string? FieldsJson,
    [property: MaxLength(1000)] string? Notes
);

public record TemplateDto(
    Guid Id,
    string Name,
    string DocumentType,
    string Language,
    string FieldsJson,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
)
{
    public static TemplateDto From(Template t) => new(
        t.Id, t.Name, t.DocumentType, t.Language, t.FieldsJson, t.Notes, t.CreatedAt, t.UpdatedAt
    );
}

public record RecognitionRunResponse(
    Guid RecognitionId,
    string TemplateName,
    string ModelName,
    string Markdown,
    string FieldsJson,
    DateTimeOffset CreatedAt
);
