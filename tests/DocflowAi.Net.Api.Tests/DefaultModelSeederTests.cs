using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Application.Abstractions;
using System.Net.Http.Json;
using System.Collections.Generic;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class DefaultModelSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public DefaultModelSeederTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Seeds_Default_Model_When_Missing()
    {
        var extra = new Dictionary<string, string?>
        {
            ["LLM:DefaultModelRepo"] = "repo/name",
            ["LLM:DefaultModelFile"] = "model.gguf",
            ["JobQueue:SeedDefaults"] = "true"
        };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var models = await client.GetFromJsonAsync<List<ModelDto>>("/api/models");
        models!.Should().ContainSingle(m => m.HfRepo == "repo/name" && m.ModelFile == "model.gguf");
    }

    [Fact]
    public async Task Does_Not_Seed_When_Disabled()
    {
        var extra = new Dictionary<string, string?>
        {
            ["LLM:DefaultModelRepo"] = "repo/name",
            ["LLM:DefaultModelFile"] = "model.gguf",
            ["JobQueue:SeedDefaults"] = "false"
        };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var models = await client.GetFromJsonAsync<List<ModelDto>>("/api/models");
        models!.Should().BeEmpty();
    }
}

