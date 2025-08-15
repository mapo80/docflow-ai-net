using System.Net;
using System.Net.Http.Json;
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
    public async Task Switch_200_Ok()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var req = new { modelFile = "f", contextSize = 10 };
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Switch_Validation_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", new { modelFile = "", contextSize = 0 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        err!.Error.Should().Be("bad_request");
    }

    [Fact]
    public async Task Switch_404_NotFound()
    {
        var fake = new ConfigurableFakeLlmModelService();
        fake.FailWith(new FileNotFoundException("missing"));
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService>(fake)));
        var client = CreateClient(factory);
        var req = new { modelFile = "bad", contextSize = 10 };
        var resp = await client.PostAsJsonAsync("/api/v1/model/switch", req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
