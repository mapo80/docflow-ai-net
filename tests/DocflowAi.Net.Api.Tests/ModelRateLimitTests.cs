using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelRateLimitTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelRateLimitTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Switch_RateLimited_429()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath, permit:1, windowSeconds:60)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { modelFile = "f", contextSize = 10 };
        await client.PostAsJsonAsync("/api/v1/model/switch", req);
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        resp.Headers.TryGetValues("Retry-After", out var vals).Should().BeTrue();
    }
}

