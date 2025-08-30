using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","FuzzEndpoints")]
public class FuzzEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public FuzzEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Preview_and_import_fuzz_tests()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        Guid ruleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            var rule = new RuleFunction { Name = "f", Code = "if(has(\"amount\")){var a=get<int>(\"amount\");}", CodeHash = "h" };
            db.RuleFunctions.Add(rule);
            await db.SaveChangesAsync();
            ruleId = rule.Id;
        }

        var previewResp = await client.PostAsync($"/api/v1/rules/{ruleId}/fuzz/preview", null);
        previewResp.EnsureSuccessStatusCode();
        var preview = await previewResp.Content.ReadFromJsonAsync<JsonObject>();
        var items = preview!["items"]!.AsArray();
        items.Count.Should().BeGreaterThan(0);

        var before = await GetCount(factory);
        var importResp = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/fuzz/import", new { items });
        importResp.EnsureSuccessStatusCode();
        var import = await importResp.Content.ReadFromJsonAsync<JsonObject>();
        import!["imported"]!.GetValue<int>().Should().Be(items.Count);

        var after = await GetCount(factory);
        after.Should().Be(before + items.Count);
    }

    [Fact]
    public async Task Preview_missing_rule_returns_empty()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PostAsync($"/api/v1/rules/{Guid.NewGuid()}/fuzz/preview", null);
        resp.EnsureSuccessStatusCode();
        var obj = await resp.Content.ReadFromJsonAsync<JsonObject>();
        obj!["items"]!.AsArray().Count.Should().Be(0);
    }

    private static async Task<int> GetCount(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        return await db.RuleTestCases.CountAsync();
    }
}
