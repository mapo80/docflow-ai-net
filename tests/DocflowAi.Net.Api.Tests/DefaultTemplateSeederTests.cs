using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Application.Abstractions;
using System.Net.Http.Json;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class DefaultTemplateSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public DefaultTemplateSeederTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Seeds_Default_Template_When_Enabled()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var list = await client.GetFromJsonAsync<PagedResult<TemplateSummary>>("/api/templates");
        list!.Items.Should().ContainSingle(t => t.Token == "template");
        var tplId = list.Items.Single(t => t.Token == "template").Id;
        var tpl = await client.GetFromJsonAsync<TemplateDto>($"/api/templates/{tplId}");
        tpl!.PromptMarkdown.Should().NotBeNullOrWhiteSpace();
        tpl.FieldsJson.EnumerateArray().Any(f => f.GetProperty("name").GetString() == "invoice_number").Should().BeTrue();
    }

    [Fact]
    public async Task Does_Not_Seed_When_Disabled()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "false"
        };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        var list = await client.GetFromJsonAsync<PagedResult<TemplateSummary>>("/api/templates");
        list!.Items.Should().BeEmpty();
    }
}

