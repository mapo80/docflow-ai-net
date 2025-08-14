using System;
using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using Serilog.Sinks.TestCorrelator;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class RateLimitSubmitTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RateLimitSubmitTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Second_submit_is_rate_limited()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath, permit:1, windowSeconds:1);
        var client = factory.CreateClient();
        var body = new { fileBase64 = Convert.ToBase64String(new byte[10]), fileName = "a.pdf" };
        await client.PostAsJsonAsync("/v1/jobs", body);
        using (TestCorrelator.CreateContext())
        {
            var resp = await client.PostAsJsonAsync("/v1/jobs", body);
            resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            resp.Headers.Should().ContainKey("Retry-After");
        }
    }
}
