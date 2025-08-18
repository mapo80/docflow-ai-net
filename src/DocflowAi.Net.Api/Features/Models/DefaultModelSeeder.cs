using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DocflowAi.Net.Api.Features.Models;

public static class DefaultModelSeeder
{
    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var seed = cfg.GetSection("ModelCatalog").GetValue<bool>("SeedDefaults");
        if (!seed) return;

        var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
        db.Database.EnsureCreated();
        if (db.Models.Any()) return;

        var storageRoot = cfg.GetSection("ModelStorage").GetValue<string>("Root") ?? "models";
        Directory.CreateDirectory(storageRoot);
        var fileName = "Qwen3-0.6B-Q4_0.gguf";
        var path = Path.Combine(storageRoot, fileName);

        var model = new GgufModel
        {
            Name = "Qwen3-0.6B",
            SourceType = ModelSourceType.HuggingFace,
            HfRepo = "unsloth/Qwen3-0.6B-GGUF",
            HfFilename = fileName,
            Status = File.Exists(path) ? ModelStatus.Available : ModelStatus.NotDownloaded,
            LocalPath = File.Exists(path) ? Path.GetFullPath(path) : null,
            DownloadProgress = File.Exists(path) ? 100 : 0,
            FileSize = File.Exists(path) ? new FileInfo(path).Length : null
        };

        db.Models.Add(model);
        db.SaveChanges();

        if (!File.Exists(path))
        {
            var worker = scope.ServiceProvider.GetRequiredService<ModelDownloadWorker>();
            worker.Enqueue(model.Id);
        }
    }
}
