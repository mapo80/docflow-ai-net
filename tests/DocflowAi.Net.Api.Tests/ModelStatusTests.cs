using System.Net.Http.Json;
using System.Text.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelStatusTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelStatusTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Status_200_ShapeOk()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var resp = await client.GetFromJsonAsync<JsonElement>("/api/v1/model/status");
        resp.GetProperty("completed").GetBoolean().Should().BeTrue();
        resp.GetProperty("percentage").GetDouble().Should().BeInRange(0,100);
    }

    [Fact]
    public async Task Status_WhileRunning()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f" };
        await client.PostAsJsonAsync("/api/v1/model/download", req);
        double last = 0;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(30);
            var s = await client.GetFromJsonAsync<JsonElement>("/api/v1/model/status");
            var pct = s.GetProperty("percentage").GetDouble();
            pct.Should().BeGreaterThanOrEqualTo(last);
            last = pct;
            if (s.GetProperty("completed").GetBoolean())
                break;
        }
        last.Should().Be(100);
    }
}

