using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelSwaggerContractTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelSwaggerContractTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Swagger_Paths_Present()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = CreateClient(factory);
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        json.Should().Contain("/api/v1/model/status");
        json.Should().Contain("/api/v1/model/switch");
        json.Should().NotContain("ModelController");
    }
}

