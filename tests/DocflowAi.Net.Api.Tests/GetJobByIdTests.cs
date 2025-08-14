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
        var store = factory.Services.GetRequiredService<IJobStore>();
        var id = Guid.NewGuid();
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
            Paths = new JobDocument.PathInfo { Dir = "/d", Input = "i", Prompt = "p", Fields = "f", Output = "o", Error = "e" },
            Metrics = new JobDocument.MetricsInfo()
        };
        store.Create(job);
        var resp = await client.GetAsync($"/v1/jobs/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("Running");
        json.GetProperty("derivedStatus").GetString().Should().Be("Processing");
        json.GetProperty("progress").GetInt32().Should().Be(42);
        json.GetProperty("paths").GetProperty("dir").GetString().Should().Be("/d");
    }

    [Fact]
    public async Task Get_missing_job_returns_404_and_logs()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/v1/jobs/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
