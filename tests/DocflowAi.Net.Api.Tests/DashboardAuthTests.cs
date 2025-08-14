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
    public async Task Dashboard_requires_auth_when_enabled()
    {
        var extra = new Dictionary<string,string?>
        {
            ["JobQueue:EnableDashboard"] = "true",
            ["HangfireDashboardAuth:Enabled"] = "true",
            ["HangfireDashboardAuth:Username"] = "u",
            ["HangfireDashboardAuth:Password"] = "p"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/hangfire");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
