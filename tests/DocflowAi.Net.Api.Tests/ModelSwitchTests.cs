using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelSwitchTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelSwitchTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Switch_200_Started()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 };
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await client.GetFromJsonAsync<JsonElement>("/api/v1/model/status");
        status.GetProperty("completed").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Switch_Validation_400_MissingFields()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", new { hfKey = "", modelRepo = "", modelFile = "", contextSize = 0 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        err!.Error.Should().Be("bad_request");
    }

    [Fact]
    public async Task Switch_Conflict_409_AlreadyRunning()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 };
        var first = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Switch_404_InvalidRepoOrFile()
    {
        var fake = new ConfigurableFakeLlmModelService();
        fake.FailWith(new FileNotFoundException("missing"));
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService>(fake)));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "bad", contextSize = 10 };
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Switch_Completes_To_100()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 };
        await client.PostAsJsonAsync("/api/v1/model/switch", req);
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(30);
            var s = await client.GetFromJsonAsync<JsonElement>("/api/v1/model/status");
            if (s.GetProperty("completed").GetBoolean())
            {
                s.GetProperty("percentage").GetDouble().Should().Be(100);
                return;
            }
        }
        Assert.Fail("Switch did not complete in time");
    }
}

