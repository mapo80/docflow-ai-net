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

        if (db.Templates.Any(t => t.Token == "template"))
            return;

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var datasetRoot = FindDatasetRoot(app.Environment.ContentRootPath)
            ?? FindDatasetRoot(AppContext.BaseDirectory);

        string? prompt = null;
        if (datasetRoot != null)
        {
            var promptPath = Path.Combine(datasetRoot, "prompt.txt");
            if (File.Exists(promptPath))
                prompt = File.ReadAllText(promptPath);
        }

        var fields = new
        {
            company_name = "",
            document_type = "",
            invoice_number = "",
            invoice_date = ""
        };

        var now = DateTimeOffset.UtcNow;
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
        db.SaveChanges();
        logger.LogInformation("SeededDefaultTemplate {Name}", tpl.Name);
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

