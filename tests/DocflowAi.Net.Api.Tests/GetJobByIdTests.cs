using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocflowAi.Net.Api.Tests;

public class GetJobByIdTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public GetJobByIdTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Get_returns_job_from_db_with_derived_status()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var id = Guid.NewGuid();
        var dir = Path.Combine(_fx.RootPath, "job");
        Directory.CreateDirectory(dir);
        var outputPath = Path.Combine(dir, "o.json");
        await File.WriteAllTextAsync(outputPath, "{}");
        var now = DateTimeOffset.UtcNow;
            var job = new JobDocument
            {
                Id = id,
                Status = "Running",
                Progress = 42,
                Attempts = 0,
                Priority = 0,
                CreatedAt = now,
                UpdatedAt = now,
                Hash = "h",
                Model = "m",
                TemplateToken = "t",
                Language = "eng",
                MarkdownSystemId = Guid.NewGuid(),
                MarkdownSystemName = "docling",
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "i.pdf"), CreatedAt = now },
                Output = new JobDocument.DocumentInfo { Path = outputPath, CreatedAt = now },
                Error = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "e.txt") },
                Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "markdown.md") },
                Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "prompt.md") }
            },
            Metrics = new JobDocument.MetricsInfo()
        };
        store.Create(job);
        uow.SaveChanges();
        var resp = await client.GetAsync($"/api/v1/jobs/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("Running");
        json.GetProperty("derivedStatus").GetString().Should().Be("Processing");
        json.GetProperty("progress").GetInt32().Should().Be(42);
        json.GetProperty("language").GetString().Should().Be("eng");
        var paths = json.GetProperty("paths");
        paths.GetProperty("input").ValueKind.Should().Be(JsonValueKind.Null);
        paths.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        paths.GetProperty("markdown").ValueKind.Should().Be(JsonValueKind.Null);
        paths.GetProperty("prompt").ValueKind.Should().Be(JsonValueKind.Null);
        var outObj = paths.GetProperty("output");
        outObj.GetProperty("path").GetString().Should().Be($"/api/v1/jobs/{id}/files/o.json");
    }

    [Fact]
    public async Task Get_missing_job_returns_404_and_logs()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/jobs/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_running_job_with_markdown_returns_path()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var id = Guid.NewGuid();
        var dir = Path.Combine(_fx.RootPath, "jobm");
        Directory.CreateDirectory(dir);
        var mdPath = Path.Combine(dir, "markdown.md");
        await File.WriteAllTextAsync(mdPath, "# md");
        var now2 = DateTimeOffset.UtcNow;
            var job = new JobDocument
            {
                Id = id,
                Status = "Running",
                CreatedAt = now2,
                UpdatedAt = now2,
                Language = "eng",
                MarkdownSystemId = Guid.NewGuid(),
                MarkdownSystemName = "docling",
                Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "i.pdf"), CreatedAt = now2 },
                Output = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "o.json") },
                Error = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "e.txt") },
                Markdown = new JobDocument.DocumentInfo { Path = mdPath, CreatedAt = now2 },
                Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "prompt.md") }
            }
        };
        store.Create(job);
        uow.SaveChanges();
        var resp = await client.GetAsync($"/api/v1/jobs/{id}");
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("paths").GetProperty("markdown").GetProperty("path").GetString().Should().Be($"/api/v1/jobs/{id}/files/markdown.md");
    }

    [Fact]
    public async Task File_endpoint_serves_artifact()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var id = Guid.NewGuid();
        var dir = Path.Combine(_fx.RootPath, "job2");
        Directory.CreateDirectory(dir);
        var outputPath = Path.Combine(dir, "res.json");
        await File.WriteAllTextAsync(outputPath, "{\"ok\":true}");
        var now3 = DateTimeOffset.UtcNow;
            var job = new JobDocument
            {
                Id = id,
                Status = "Succeeded",
                CreatedAt = now3,
                UpdatedAt = now3,
                Hash = "h",
                Model = "m",
                TemplateToken = "t",
                Language = "eng",
                MarkdownSystemId = Guid.NewGuid(),
                MarkdownSystemName = "docling",
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "i.pdf"), CreatedAt = now3 },
                Output = new JobDocument.DocumentInfo { Path = outputPath, CreatedAt = now3 },
                Error = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "e.txt") },
                Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "markdown.md") },
                Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "prompt.md") }
            }
        };
        store.Create(job);
        uow.SaveChanges();
        var resp = await client.GetAsync($"/api/v1/jobs/{id}/files/res.json");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await resp.Content.ReadAsStringAsync();
        text.Should().Contain("ok");
    }
}
