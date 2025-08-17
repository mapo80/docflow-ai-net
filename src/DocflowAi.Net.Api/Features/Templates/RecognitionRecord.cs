using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Templates;

public class RecognitionRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ModelName { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? FileName { get; set; }

    public string Markdown { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "{}";
}

public class RecognitionsDbContext : DbContext
{
    public RecognitionsDbContext(DbContextOptions<RecognitionsDbContext> options) : base(options) { }
    public DbSet<RecognitionRecord> Recognitions => Set<RecognitionRecord>();
}
