using System.Net;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Api.Tests;

public class ModelSecurityTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelSecurityTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Requires_ApiKey()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/model/status");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/model/status");
        req.Headers.Add("X-API-Key", "dev-secret-key-change-me");
        var ok = await client.SendAsync(req);
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
