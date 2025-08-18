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
            ["JobQueue:EnableHangfireDashboard"] = "true",
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
            ["JobQueue:EnableHangfireDashboard"] = "true",
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
    public async Task Static_assets_are_served_after_initial_authorization()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:EnableHangfireDashboard"] = "true",
            ["Api:Keys:0"] = "k"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);

        var authorizedClient = factory.CreateClient();
        var resp = await authorizedClient.GetAsync("/hangfire?api_key=k");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await resp.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(content, "href=\\\"(?<p>/hangfire/[^\\\"]+)\\\"");
        match.Success.Should().BeTrue("resource path should be present");
        var assetPath = match.Groups["p"].Value;

        var unauthorizedClient = factory.CreateClient();
        var unauthorized = await unauthorizedClient.GetAsync(assetPath);
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var authorized = await authorizedClient.GetAsync(assetPath);
        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

