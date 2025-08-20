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
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var id = Guid.NewGuid();
        var dir = Path.Combine(_fx.RootPath, "job");
        Directory.CreateDirectory(dir);
        var outputPath = Path.Combine(dir, "o.json");
        await File.WriteAllTextAsync(outputPath, "{}");
        var job = new JobDocument
        {
            Id = id,
            Status = "Running",
            Progress = 42,
            Attempts = 0,
            Priority = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Hash = "h",
            Model = "m",
            TemplateToken = "t",
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = Path.Combine(dir, "i.pdf"),
                Output = outputPath,
                Error = Path.Combine(dir, "e.txt"),
                Markdown = Path.Combine(dir, "markdown.md")
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
        var paths = json.GetProperty("paths");
        paths.GetProperty("input").ValueKind.Should().Be(JsonValueKind.Null);
        paths.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        paths.GetProperty("markdown").ValueKind.Should().Be(JsonValueKind.Null);
        paths
            .GetProperty("output")
            .GetString()
            .Should()
            .Be($"/api/v1/jobs/{id}/files/o.json");
    }

    [Fact]
    public async Task Get_missing_job_returns_404_and_logs()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
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
        var job = new JobDocument
        {
            Id = id,
            Status = "Running",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = Path.Combine(dir, "i.pdf"),
                Output = Path.Combine(dir, "o.json"),
                Error = Path.Combine(dir, "e.txt"),
                Markdown = mdPath
            }
        };
        store.Create(job);
        uow.SaveChanges();
        var resp = await client.GetAsync($"/api/v1/jobs/{id}");
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("paths").GetProperty("markdown").GetString().Should().Be($"/api/v1/jobs/{id}/files/markdown.md");
    }

    [Fact]
    public async Task File_endpoint_serves_artifact()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var id = Guid.NewGuid();
        var dir = Path.Combine(_fx.RootPath, "job2");
        Directory.CreateDirectory(dir);
        var outputPath = Path.Combine(dir, "res.json");
        await File.WriteAllTextAsync(outputPath, "{\"ok\":true}");
        var job = new JobDocument
        {
            Id = id,
            Status = "Succeeded",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Hash = "h",
            Model = "m",
            TemplateToken = "t",
            Paths = new JobDocument.PathInfo { Dir = dir, Output = outputPath, Markdown = Path.Combine(dir, "markdown.md") }
        };
        store.Create(job);
        uow.SaveChanges();
        var resp = await client.GetAsync($"/api/v1/jobs/{id}/files/res.json");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await resp.Content.ReadAsStringAsync();
        text.Should().Contain("ok");
    }
}
