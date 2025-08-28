namespace DocflowAi.Net.Api.Data;

using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using DocflowAi.Net.Api.MarkdownSystem.Models;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using DocflowAi.Net.Api.JobQueue.Data;

/// <summary>
/// Seeds models and markdown systems from configuration.
/// </summary>
public static class AppSettingsSeeder
{
    public sealed record ModelSeed
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string? Provider { get; init; }
        public string? BaseUrl { get; init; }
        public string? ApiKey { get; init; }
        public string? HfRepo { get; init; }
        public string? ModelFile { get; init; }
        public string? HfToken { get; init; }
    }

    public sealed record MarkdownSystemSeed
    {
        public string Name { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string Endpoint { get; init; } = string.Empty;
        public string? ApiKey { get; init; }
    }

    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IOptions<JobQueueOptions>>().Value;
        if (!cfg.SeedDefaults)
            return;

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();
        var seedSection = app.Configuration.GetSection("Seed");

        var modelSeeds = seedSection.GetSection("Models").Get<ModelSeed[]>() ?? Array.Empty<ModelSeed>();
        if (modelSeeds.Length > 0)
        {
            var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            foreach (var m in modelSeeds)
            {
                if (repo.ExistsByName(m.Name))
                    continue;
                var doc = new ModelDocument
                {
                    Id = Guid.NewGuid(),
                    Name = m.Name,
                    Type = m.Type,
                    Provider = m.Provider,
                    BaseUrl = m.BaseUrl,
                    HfRepo = m.HfRepo,
                    ModelFile = m.ModelFile,
                    DownloadStatus = m.Type == "local" ? "NotRequested" : null,
                    Downloaded = m.Type == "local" ? false : (bool?)null,
                    IsActive = true,
                };
                repo.Add(doc, m.ApiKey, m.HfToken);
                logger.LogInformation("SeededModel {Name}", m.Name);
            }
            repo.SaveChanges();
        }

        var systemSeeds = seedSection.GetSection("MarkdownSystems").Get<MarkdownSystemSeed[]>() ?? Array.Empty<MarkdownSystemSeed>();
        if (systemSeeds.Length > 0)
        {
            var repo = scope.ServiceProvider.GetRequiredService<IMarkdownSystemRepository>();
            foreach (var s in systemSeeds)
            {
                if (repo.ExistsByName(s.Name))
                    continue;
                var doc = new MarkdownSystemDocument
                {
                    Id = Guid.NewGuid(),
                    Name = s.Name,
                    Provider = s.Provider,
                    Endpoint = s.Endpoint,
                };
                repo.Add(doc, s.ApiKey);
                logger.LogInformation("SeededMarkdownSystem {Name}", s.Name);
            }
            repo.SaveChanges();
        }
    }
}

