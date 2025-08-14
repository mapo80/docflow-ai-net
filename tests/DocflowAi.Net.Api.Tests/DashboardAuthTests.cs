using System.Net;
using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.Tests;

public class DashboardAuthTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public DashboardAuthTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Dashboard_requires_api_key_when_enabled()
    {
        var extra = new Dictionary<string,string?>
        {
            ["JobQueue:EnableDashboard"] = "true",
            ["HangfireDashboardAuth:Enabled"] = "true",
            ["Api:Keys:0"] = "k"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/hangfire");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_allows_request_with_valid_api_key()
    {
        var extra = new Dictionary<string,string?>
        {
            ["JobQueue:EnableDashboard"] = "true",
            ["HangfireDashboardAuth:Enabled"] = "true",
            ["Api:Keys:0"] = "k"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/hangfire?api_key=k");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
