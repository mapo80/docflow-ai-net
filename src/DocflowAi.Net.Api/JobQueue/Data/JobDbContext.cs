using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Api.Templates.Models;
using DocflowAi.Net.Api.MarkdownSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocflowAi.Net.Api.JobQueue.Data;

public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options) { }

    public DbSet<JobDocument> Jobs => Set<JobDocument>();
    public DbSet<ModelDocument> Models => Set<ModelDocument>();
    public DbSet<TemplateDocument> Templates => Set<TemplateDocument>();
    public DbSet<MarkdownSystemDocument> MarkdownSystems => Set<MarkdownSystemDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<JobDocument>();
        var converter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        var nullableConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null);

        job.HasKey(j => j.Id);
        job.HasIndex(j => j.CreatedAt);
        job.HasIndex(j => j.Status);
        job.HasIndex(j => j.IdempotencyKey);
        job.HasIndex(j => j.Hash);
        job.HasIndex(j => j.Language);
        job.HasIndex(j => j.MarkdownSystemId);
        job.Property(j => j.MarkdownSystemName).IsRequired();

        job.Property(j => j.CreatedAt).HasConversion(converter);
        job.Property(j => j.UpdatedAt).HasConversion(converter);
        job.Property(j => j.Language).IsRequired();
        

        job.OwnsOne(j => j.Metrics, m =>
        {
            m.Property(x => x.StartedAt).HasConversion(nullableConverter);
            m.Property(x => x.EndedAt).HasConversion(nullableConverter);
        });
        job.OwnsOne(j => j.Paths, pi =>
        {
            pi.OwnsOne(p => p.Input, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.Prompt, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.Output, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.Error, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.Markdown, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.Layout, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
            pi.OwnsOne(p => p.LayoutOutput, d => d.Property(x => x.CreatedAt).HasConversion(nullableConverter));
        });

        var model = modelBuilder.Entity<ModelDocument>();
        model.HasKey(m => m.Id);
        model.HasIndex(m => m.Name).IsUnique();
        model.Property(m => m.CreatedAt).HasConversion(converter);
        model.Property(m => m.UpdatedAt).HasConversion(converter);
        model.Property(m => m.LastUsedAt).HasConversion(nullableConverter);
        model.Property(m => m.DownloadedAt).HasConversion(nullableConverter);

        var template = modelBuilder.Entity<TemplateDocument>();
        template.HasKey(t => t.Id);
        template.HasIndex(t => t.Name).IsUnique();
        template.HasIndex(t => t.Token).IsUnique();
        template.Property(t => t.Name).HasMaxLength(200).IsRequired();
        template.Property(t => t.Token).HasMaxLength(100).IsRequired();
        template.Property(t => t.FieldsJson).IsRequired();
        template.Property(t => t.CreatedAt).HasConversion(converter);
        template.Property(t => t.UpdatedAt).HasConversion(converter);

        var ms = modelBuilder.Entity<MarkdownSystemDocument>();
        ms.HasKey(m => m.Id);
        ms.HasIndex(m => m.Name).IsUnique();
        ms.Property(m => m.Name).HasMaxLength(200).IsRequired();
        ms.Property(m => m.Provider).HasMaxLength(100).IsRequired();
        ms.Property(m => m.Endpoint).IsRequired();
        ms.Property(m => m.CreatedAt).HasConversion(converter);
        ms.Property(m => m.UpdatedAt).HasConversion(converter);
    }
}
