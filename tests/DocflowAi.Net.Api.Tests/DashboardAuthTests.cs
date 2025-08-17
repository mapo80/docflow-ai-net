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
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:EnableDashboard"] = "true",
            ["HangfireDashboardAuth:Enabled"] = "true",
            ["Api:Keys:0"] = "k"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/hangfire?api_key=k");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().Contain("Hangfire", "dashboard should contain Hangfire information");
    }

    [Fact]
    public async Task Static_assets_require_api_key_in_query_string()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:EnableDashboard"] = "true",
            ["HangfireDashboardAuth:Enabled"] = "true",
            ["Api:Keys:0"] = "k"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/hangfire?api_key=k");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await resp.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(content, "href=\"(?<p>/hangfire/[^\"]+)\"");
        match.Success.Should().BeTrue("resource path should be present");
        var assetPath = match.Groups["p"].Value;
        var unauthorized = await client.GetAsync(assetPath);
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var authorized = await client.GetAsync(assetPath + "?api_key=k");
        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
