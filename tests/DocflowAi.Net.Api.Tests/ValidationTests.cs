using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;
using System.Net;
using System.Net.Http.Json;

namespace DocflowAi.Net.Api.Tests;

public class ValidationTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ValidationTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Payload_too_large_returns_413()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath, uploadLimitMb:1);
        var client = factory.CreateClient();
        var big = new byte[2 * 1024 * 1024];
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(big), fileName = "a.pdf", model = "m", templateToken = "t", language = "eng" });
        resp.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        Directory.GetDirectories(factory.DataRootPath).Should().BeEmpty();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        store.CountPending().Should().Be(0);
    }

    [Fact]
    public async Task Invalid_mime_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[10];
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(bytes), fileName = "a.exe", model = "m", templateToken = "t", language = "eng" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Directory.GetDirectories(factory.DataRootPath).Should().BeEmpty();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        store.CountPending().Should().Be(0);
    }

    [Fact]
    public async Task Missing_language_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[10];
        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(bytes), fileName = "a.pdf", model = "m", templateToken = "t" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
