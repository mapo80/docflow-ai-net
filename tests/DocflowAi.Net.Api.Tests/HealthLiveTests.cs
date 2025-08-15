using System.Net;
using DocflowAi.Net.Api.Tests.Fixtures;
using System.Net.Http.Json;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class HealthLiveTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;
    public HealthLiveTests(TempDirFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Live_ReturnsOk()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/live");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<LiveResp>();
        json!.status.Should().Be("ok");
    }

    private record LiveResp(string status);
}
