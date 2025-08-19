using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using System.IO;
using System;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelAvailableTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelAvailableTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Available_Returns_Gguf_Files()
    {
        var modelsDir = Path.Combine(_fx.RootPath, "models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "a.gguf"), "");
        File.WriteAllText(Path.Combine(modelsDir, "b.txt"), "");
        var prev = Environment.GetEnvironmentVariable("MODELS_DIR");
        Environment.SetEnvironmentVariable("MODELS_DIR", modelsDir);
        try
        {
            await using var factory = new TestWebAppFactory(_fx.RootPath);
            var client = CreateClient(factory);
            var resp = await client.GetFromJsonAsync<string[]>("/api/v1/model/available");
            resp.Should().Contain("a.gguf");
            resp.Should().HaveCount(1);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MODELS_DIR", prev);
        }
    }

}
