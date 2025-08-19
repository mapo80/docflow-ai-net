namespace DocflowAi.Net.Api.Templates.Data;

using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.Templates.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using System.IO;
using System;
using System.Linq;

public static class DefaultTemplateSeeder
{
    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IOptions<JobQueueOptions>>().Value;
        if (!cfg.SeedDefaults)
            return;

        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var datasetRoot = FindDatasetRoot(app.Environment.ContentRootPath)
            ?? FindDatasetRoot(AppContext.BaseDirectory);

        var existing = db.Templates.Select(t => t.Token).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        if (!existing.Contains("template"))
        {
            string? prompt = null;
            if (datasetRoot != null)
            {
                var promptPath = Path.Combine(datasetRoot, "prompt.txt");
                if (File.Exists(promptPath))
                    prompt = File.ReadAllText(promptPath);
            }

            var fields = new[]
            {
                new { Key = "company_name", Description = "", Type = "string", Required = false },
                new { Key = "document_type", Description = "", Type = "string", Required = false },
                new { Key = "invoice_number", Description = "", Type = "string", Required = false },
                new { Key = "invoice_date", Description = "", Type = "string", Required = false }
            };

            var tpl = new TemplateDocument
            {
                Id = Guid.NewGuid(),
                Name = "Sample invoice",
                Token = "template",
                PromptMarkdown = prompt,
                FieldsJson = JsonSerializer.Serialize(fields),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Templates.Add(tpl);
            logger.LogInformation("SeededDefaultTemplate {Name}", tpl.Name);
        }

        if (!existing.Contains("busta-paga"))
        {
            string? bpPrompt = null;
            string bpFields = "[]";
            if (datasetRoot != null)
            {
                var bpDir = Path.Combine(datasetRoot, "busta-paga");
                var pp = Path.Combine(bpDir, "prompt.txt");
                if (File.Exists(pp))
                    bpPrompt = File.ReadAllText(pp);
                var fp = Path.Combine(bpDir, "fields.txt");
                if (File.Exists(fp))
                    bpFields = File.ReadAllText(fp);
            }

            var tpl = new TemplateDocument
            {
                Id = Guid.NewGuid(),
                Name = "Busta paga",
                Token = "busta-paga",
                PromptMarkdown = bpPrompt,
                FieldsJson = bpFields,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Templates.Add(tpl);
            logger.LogInformation("SeededDefaultTemplate {Name}", tpl.Name);
        }

        db.SaveChanges();
    }

    private static string? FindDatasetRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "dataset");
            if (Directory.Exists(path))
                return path;
            dir = dir.Parent;
        }
        return null;
    }
}
