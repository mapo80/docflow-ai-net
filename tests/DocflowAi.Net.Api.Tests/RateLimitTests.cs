using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;

namespace DocflowAi.Net.Api.Tests;

public class RateLimitTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;
    public RateLimitTests(TempDirFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExceedingLimit_Returns429()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath, permit:2, windowSeconds:1);
        var client = factory.CreateClient();
        await client.GetAsync("/api/v1/jobs");
        await client.GetAsync("/api/v1/jobs");
        var resp = await client.GetAsync("/api/v1/jobs");
        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var json = await resp.Content.ReadFromJsonAsync<RateLimitResponse>();
        json!.error.Should().Be("rate_limited");
        json.retry_after_seconds.Should().BeGreaterThanOrEqualTo(0);
    }

    private record RateLimitResponse(string error, int retry_after_seconds);
}
