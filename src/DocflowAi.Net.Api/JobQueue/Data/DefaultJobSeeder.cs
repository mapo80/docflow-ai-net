namespace DocflowAi.Net.Api.JobQueue.Data;

using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.JobQueue.Models;
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
                var ms = db.MarkdownSystems.FirstOrDefault();
                var msId = ms?.Id ?? Guid.Empty;
                var msName = ms?.Name ?? "markdown";

                // succeeded job
                var jobId = Guid.Parse("33333333-3333-3333-3333-333333333333");
                var jobDir = Path.Combine(cfg.DataRoot, jobId.ToString());
                Directory.CreateDirectory(jobDir);
                var seedDir = Path.Combine(datasetRoot, "job-seed");
                File.Copy(Path.Combine(seedDir, "input.pdf"), Path.Combine(jobDir, "input.pdf"), true);
                File.Copy(Path.Combine(seedDir, "prompt.md"), Path.Combine(jobDir, "prompt.md"), true);
                File.Copy(Path.Combine(seedDir, "output.json"), Path.Combine(jobDir, "output.json"), true);
                File.Copy(Path.Combine(seedDir, "markdown.md"), Path.Combine(jobDir, "markdown.md"), true);
                File.Copy(Path.Combine(seedDir, "layout.json"), Path.Combine(jobDir, "layout.json"), true);
                File.Copy(Path.Combine(seedDir, "output-layout.json"), Path.Combine(jobDir, "output-layout.json"), true);

                var job = new JobDocument
                {
                    Id = jobId,
                    Status = "Succeeded",
                    Progress = 100,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Metrics = new JobDocument.MetricsInfo { StartedAt = now, EndedAt = now, DurationMs = 0 },
                    Model = modelName,
                    TemplateToken = templateToken,
                    Language = "ita",
                    MarkdownSystemId = msId,
                    MarkdownSystemName = msName,
                    Paths = new JobDocument.PathInfo
                    {
                        Dir = jobDir,
                        Input = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "input.pdf"), CreatedAt = now },
                        Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "prompt.md"), CreatedAt = now },
                        Output = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "output.json"), CreatedAt = now },
                        Error = new JobDocument.DocumentInfo { Path = string.Empty },
                        Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "markdown.md"), CreatedAt = now },
                        Layout = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "layout.json"), CreatedAt = now },
                        LayoutOutput = new JobDocument.DocumentInfo { Path = Path.Combine(jobDir, "output-layout.json"), CreatedAt = now }
                    }
                };

                db.Jobs.Add(job);

                // succeeded image job
                var imgJobId = Guid.Parse("55555555-5555-5555-5555-555555555555");
                var imgDir = Path.Combine(cfg.DataRoot, imgJobId.ToString());
                Directory.CreateDirectory(imgDir);
                var imgSeedDir = Path.Combine(datasetRoot, "job-seed-png");
                File.Copy(Path.Combine(imgSeedDir, "input.png"), Path.Combine(imgDir, "input.png"), true);
                File.Copy(Path.Combine(imgSeedDir, "prompt.md"), Path.Combine(imgDir, "prompt.md"), true);
                File.Copy(Path.Combine(imgSeedDir, "output.json"), Path.Combine(imgDir, "output.json"), true);
                File.Copy(Path.Combine(imgSeedDir, "markdown.md"), Path.Combine(imgDir, "markdown.md"), true);
                File.Copy(Path.Combine(imgSeedDir, "layout.json"), Path.Combine(imgDir, "layout.json"), true);
                File.Copy(Path.Combine(imgSeedDir, "output-layout.json"), Path.Combine(imgDir, "output-layout.json"), true);

                var imgJob = new JobDocument
                {
                    Id = imgJobId,
                    Status = "Succeeded",
                    Progress = 100,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Metrics = new JobDocument.MetricsInfo { StartedAt = now, EndedAt = now, DurationMs = 0 },
                    Model = modelName,
                    TemplateToken = templateToken,
                    Language = "ita",
                    MarkdownSystemId = msId,
                    MarkdownSystemName = msName,
                    Paths = new JobDocument.PathInfo
                    {
                        Dir = imgDir,
                        Input = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "input.png"), CreatedAt = now },
                        Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "prompt.md"), CreatedAt = now },
                        Output = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "output.json"), CreatedAt = now },
                        Error = new JobDocument.DocumentInfo { Path = string.Empty },
                        Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "markdown.md"), CreatedAt = now },
                        Layout = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "layout.json"), CreatedAt = now },
                        LayoutOutput = new JobDocument.DocumentInfo { Path = Path.Combine(imgDir, "output-layout.json"), CreatedAt = now }
                    }
                };

                db.Jobs.Add(imgJob);

                // failed job
                var errJobId = Guid.Parse("44444444-4444-4444-4444-444444444444");
                var errDir = Path.Combine(cfg.DataRoot, errJobId.ToString());
                Directory.CreateDirectory(errDir);
                var errSeedDir = Path.Combine(datasetRoot, "job-error");
                File.Copy(Path.Combine(errSeedDir, "input.pdf"), Path.Combine(errDir, "input.pdf"), true);
                File.Copy(Path.Combine(errSeedDir, "prompt.md"), Path.Combine(errDir, "prompt.md"), true);
                File.Copy(Path.Combine(errSeedDir, "markdown.md"), Path.Combine(errDir, "markdown.md"), true);
                File.Copy(Path.Combine(errSeedDir, "layout.json"), Path.Combine(errDir, "layout.json"), true);
                File.Copy(Path.Combine(errSeedDir, "output-layout.json"), Path.Combine(errDir, "output-layout.json"), true);
                File.Copy(Path.Combine(errSeedDir, "error.txt"), Path.Combine(errDir, "error.txt"), true);

                var errJob = new JobDocument
                {
                    Id = errJobId,
                    Status = "Failed",
                    Progress = 100,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ErrorMessage = "Mock extraction error",
                    Metrics = new JobDocument.MetricsInfo { StartedAt = now, EndedAt = now, DurationMs = 0 },
                    Model = modelName,
                    TemplateToken = templateToken,
                    Language = "ita",
                    MarkdownSystemId = msId,
                    MarkdownSystemName = msName,
                    Paths = new JobDocument.PathInfo
                    {
                        Dir = errDir,
                        Input = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "input.pdf"), CreatedAt = now },
                        Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "prompt.md"), CreatedAt = now },
                        Output = new JobDocument.DocumentInfo { Path = string.Empty },
                        Error = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "error.txt"), CreatedAt = now },
                        Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "markdown.md"), CreatedAt = now },
                        Layout = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "layout.json"), CreatedAt = now },
                        LayoutOutput = new JobDocument.DocumentInfo { Path = Path.Combine(errDir, "output-layout.json"), CreatedAt = now }
                    }
                };

                db.Jobs.Add(errJob);

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
