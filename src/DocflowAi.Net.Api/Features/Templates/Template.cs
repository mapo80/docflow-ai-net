using System.ComponentModel.DataAnnotations;

namespace DocflowAi.Net.Api.Features.Templates;

public class Template
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DocumentType { get; set; } = "generic";

    [MaxLength(20)]
    public string Language { get; set; } = "auto";

    /// <summary>
    /// JSON array of FieldSpec { key, description, type, required }
    /// </summary>
    public string FieldsJson { get; set; } = "[]";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
