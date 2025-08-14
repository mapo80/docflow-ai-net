using System.Net.Http.Json;
using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Infrastructure.Llm;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpointsE2E")]
public class ModelEndpointsE2E : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelEndpointsE2E(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Switch_Completes_WithRealService()
    {
        var hfToken = Environment.GetEnvironmentVariable("HF_TOKEN");
        var repo = Environment.GetEnvironmentVariable("HF_REPO");
        var file = Environment.GetEnvironmentVariable("HF_MODEL_FILE");
        var ctx = Environment.GetEnvironmentVariable("CONTEXT_SIZE");
        if (string.IsNullOrWhiteSpace(hfToken) || string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(ctx))
            return; // env vars not set, skip
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                s.AddHttpClient<ILlmModelService, LlmModelService>();
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var req = new SwitchModelRequest
        {
            HfKey = hfToken!,
            ModelRepo = repo!,
            ModelFile = file!,
            ContextSize = int.Parse(ctx!)
        };
        await client.PostAsJsonAsync("/api/v1/model/switch", req);
        for (int i = 0; i < 600; i++)
        {
            await Task.Delay(1000);
            var status = await client.GetFromJsonAsync<ModelDownloadStatus>("/api/v1/model/status");
            if (status!.Completed && status.Percentage >= 100)
                return;
        }
        Assert.Fail("switch did not complete");
    }
}

