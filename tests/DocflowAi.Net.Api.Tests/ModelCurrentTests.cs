using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelCurrentTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelCurrentTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Current_Returns_ModelInfo()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var svc = (ConfigurableFakeLlmModelService)factory.Services.GetRequiredService<ILlmModelService>();
        svc.SetCurrent(new ModelInfo(null, null, "a.gguf", 123, DateTime.UtcNow));
        var resp = await client.GetFromJsonAsync<ModelInfo>("/api/v1/model");
        resp!.File.Should().Be("a.gguf");
        resp.ContextSize.Should().Be(123);
    }
}
