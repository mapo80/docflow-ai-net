namespace DocflowAi.Net.Api.JobQueue.Data;

using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Application.Markdown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using System.Linq;

public static class DefaultJobSeeder
{
    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IOptions<JobQueueOptions>>().Value;
        Directory.CreateDirectory(cfg.DataRoot);
        if (cfg.Database.Provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var csb = new SqliteConnectionStringBuilder(cfg.Database.ConnectionString);
            var dbPath = csb.DataSource;
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();

        if (cfg.SeedDefaults && !db.Jobs.Any())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
            var datasetRoot = FindDatasetRoot(app.Environment.ContentRootPath)
                ?? FindDatasetRoot(AppContext.BaseDirectory);
            if (datasetRoot != null)
            {
                var now = DateTimeOffset.UtcNow;
                var modelName = db.Models.Select(m => m.Name).FirstOrDefault() ?? "model";
                var templateToken = db.Templates.Select(t => t.Token).FirstOrDefault() ?? "template";

                var okId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                var okDir = Path.Combine(cfg.DataRoot, okId.ToString());
                Directory.CreateDirectory(okDir);
                File.Copy(Path.Combine(datasetRoot, "sample_invoice.pdf"), Path.Combine(okDir, "input.pdf"), true);
                File.Copy(Path.Combine(datasetRoot, "test-png-boxsolver-pointerstrategy", "result.json"), Path.Combine(okDir, "output.json"), true);
                var okJob = new JobDocument
                {
                    Id = okId,
                    Status = "Succeeded",
                    Progress = 100,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Metrics = new JobDocument.MetricsInfo { StartedAt = now, EndedAt = now, DurationMs = 0 },
                    Model = modelName,
                    TemplateToken = templateToken,
                    Language = "eng",
                    Engine = OcrEngine.Tesseract,
                    Paths = new JobDocument.PathInfo
                    {
                        Dir = okDir,
                        Input = new JobDocument.DocumentInfo { Path = Path.Combine(okDir, "input.pdf"), CreatedAt = now },
                        Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(okDir, "prompt.md"), CreatedAt = now },
                        Output = new JobDocument.DocumentInfo { Path = Path.Combine(okDir, "output.json"), CreatedAt = now },
                        Error = new JobDocument.DocumentInfo { Path = string.Empty },
                        Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(okDir, "markdown.md"), CreatedAt = now }
                    }
                };

                var errId = Guid.Parse("22222222-2222-2222-2222-222222222222");
                var errDir = Path.Combine(cfg.DataRoot, errId.ToString());
                Directory.CreateDirectory(errDir);
                File.Copy(Path.Combine(datasetRoot, "sample_invoice.png"), Path.Combine(errDir, "input.png"), true);
                File.Copy(Path.Combine(datasetRoot, "test-png", "llm_response.txt"), Path.Combine(errDir, "error.txt"), true);
                var errJob = new JobDocument
                {
                    Id = errId,
                    Status = "Failed",
                    Progress = 0,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ErrorMessage = "Processing failed",
                    Metrics = new JobDocument.MetricsInfo(),
                    Model = modelName,
                    TemplateToken = templateToken,
                    Language = "ita",
                    Engine = OcrEngine.RapidOcr,
                    Paths = new JobDocument.PathInfo
                    {
                        Dir = errDir,
                        Input = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "input.png"), CreatedAt = now },
                        Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "prompt.md"), CreatedAt = now },
                        Output = new JobDocument.DocumentInfo { Path = string.Empty },
                        Error = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "error.txt"), CreatedAt = now },
                        Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "markdown.md"), CreatedAt = now }
                    }
                };

                db.Jobs.AddRange(okJob, errJob);
                db.SaveChanges();
                logger.LogInformation("SeededDefaultJobs");
            }
            else
            {
                logger.LogWarning("DatasetFolderMissing {Path}", "dataset");
            }
        }
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
