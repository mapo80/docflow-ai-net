namespace DocflowAi.Net.Api.Model.Data;

using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Api.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// Seeds the default model downloaded during docker build.
/// </summary>
public static class DefaultModelSeeder
{
    public static void Build(WebApplication app)
    {
        var repo = app.Configuration["LLM:DefaultModelRepo"];
        var file = app.Configuration["LLM:DefaultModelFile"];
        if (string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(file))
            return;

        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IOptions<JobQueueOptions>>().Value;
        if (!cfg.SeedDefaults)
            return;

        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();

        if (db.Models.Any(m => m.HfRepo == repo && m.ModelFile == file))
            return;

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var now = DateTimeOffset.UtcNow;
        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? "/home/appuser/models";
        var modelPath = Path.Combine(modelsDir, file);
        var model = new ModelDocument
        {
            Id = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(file),
            Type = "local",
            HfRepo = repo,
            ModelFile = file,
            DownloadStatus = "Downloaded",
            Downloaded = true,
            LocalPath = modelPath,
            DownloadedAt = now,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Models.Add(model);
        db.SaveChanges();
        logger.LogInformation("SeededDefaultModel {Name}", model.Name);
    }
}

