using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Application.Abstractions;
using System.Collections.Generic;
using System.Net.Http.Json;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class AppSettingsSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public AppSettingsSeederTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Seeds_entities_from_configuration()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true",
            ["Api:Keys:0"] = "test-key",
            ["Seed:Models:0:Name"] = "m1",
            ["Seed:Models:0:Type"] = "hosted-llm",
            ["Seed:Models:0:Provider"] = "openai",
            ["Seed:Models:0:BaseUrl"] = "https://example.com",
            ["Seed:Models:0:ApiKey"] = "k1",
            ["Seed:MarkdownSystems:0:Name"] = "doc",
            ["Seed:MarkdownSystems:0:Provider"] = "docling",
            ["Seed:MarkdownSystems:0:Endpoint"] = "http://localhost:5001"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-key");

        var models = await client.GetFromJsonAsync<List<ModelDto>>("/api/models");
        models!.Should().ContainSingle(m => m.Name == "m1" && m.Provider == "openai");

        var systems = await client.GetFromJsonAsync<List<MarkdownSystemDto>>("/api/markdown-systems");
        systems!.Should().ContainSingle(s => s.Name == "doc" && s.Provider == "docling");
    }

    [Fact]
    public async Task Does_not_seed_when_disabled()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "false",
            ["Api:Keys:0"] = "test-key",
            ["Seed:Models:0:Name"] = "m1",
            ["Seed:Models:0:Type"] = "hosted-llm",
            ["Seed:MarkdownSystems:0:Name"] = "doc",
            ["Seed:MarkdownSystems:0:Provider"] = "docling",
            ["Seed:MarkdownSystems:0:Endpoint"] = "http://localhost:5001"
        };
        using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-key");

        var models = await client.GetFromJsonAsync<List<ModelDto>>("/api/models");
        models!.Should().BeEmpty();
        var systems = await client.GetFromJsonAsync<List<MarkdownSystemDto>>("/api/markdown-systems");
        systems!.Should().BeEmpty();
    }
}
