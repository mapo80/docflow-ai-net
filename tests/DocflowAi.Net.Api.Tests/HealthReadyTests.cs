using System.Net;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.TestCorrelator;
using System.Net.Http.Json;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class HealthReadyTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;
    public HealthReadyTests(TempDirFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ready_ReturnsOk()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Fails_When_DataRoot_Missing()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        Directory.Delete(factory.DataRootPath, true);
        using (TestCorrelator.CreateContext())
        {
            var resp = await client.GetAsync("/health/ready");
            resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var json = await resp.Content.ReadFromJsonAsync<ReadyResp>();
            json!.reasons.Should().Contain("data_root_not_writable");
        }
    }

    [Fact]
    public async Task Ready_Fails_On_Backpressure()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath, maxQueueLength:1);
        var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<IJobStore>();
        store.Create(LiteDbTestHelper.CreateJob(Guid.NewGuid(), "Queued", DateTimeOffset.UtcNow));
        store.Create(LiteDbTestHelper.CreateJob(Guid.NewGuid(), "Queued", DateTimeOffset.UtcNow));
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var json = await resp.Content.ReadFromJsonAsync<ReadyResp>();
        json!.reasons.Should().Contain("backpressure");
    }

    private record ReadyResp(string status, string[] reasons);
}
