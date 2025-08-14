using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocflowAi.Net.Api.Tests;

public class ModelEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Status_200()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        var resp = await client.GetFromJsonAsync<JsonElement>("/api/v1/model/status");
        resp.GetProperty("completed").GetBoolean().Should().BeTrue();
        resp.GetProperty("percentage").GetDouble().Should().Be(100);
    }

    [Fact]
    public async Task Switch_200_And_Progress()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 };
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
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

    [Fact]
    public async Task Switch_Validation_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", new { hfKey = "", modelRepo = "", modelFile = "", contextSize = 0 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        err!.Error.Should().Be("bad_request");
    }

    [Fact]
    public async Task Switch_Conflict_409()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 };
        var first = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RateLimited_429()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath, permit:1, windowSeconds:60)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        await client.GetAsync("/api/v1/model/status");
        var resp = await client.GetAsync("/api/v1/model/status");
        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        resp.Headers.TryGetValues("Retry-After", out var vals).Should().BeTrue();
    }

    [Fact]
    public async Task Swagger_Paths_Present()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = CreateClient(factory);
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        json.Should().Contain("/api/v1/model/status");
        json.Should().Contain("/api/v1/model/switch");
        json.Should().NotContain("ModelController");
    }
}
