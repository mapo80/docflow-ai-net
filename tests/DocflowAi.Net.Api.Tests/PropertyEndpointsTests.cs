using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Services;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","PropertyEndpoints")]
public class PropertyEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public PropertyEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Run_and_import_flow()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        Guid id;
        int before;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            var rule = new RuleFunction { Name = "r", Code = "var n=get<double>(\"n\"); set(\"n\", n+1);", CodeHash = "h" };
            db.RuleFunctions.Add(rule);
            await db.SaveChangesAsync();
            id = rule.Id;
            before = await db.RuleTestCases.CountAsync();
        }

        var run = await client.PostAsync($"/api/v1/rules/{id}/properties/run", null);
        run.EnsureSuccessStatusCode();
        var res = await run.Content.ReadFromJsonAsync<PropertyRunResult>();
        res!.Failed.Should().BeGreaterThan(0);
        var failure = res.Failures.First();

        var import = await client.PostAsJsonAsync($"/api/v1/rules/{id}/properties/import-failures", new { failures = new[] { failure } });
        import.EnsureSuccessStatusCode();
        var obj = await import.Content.ReadFromJsonAsync<JsonObject>();
        obj!["imported"]!.GetValue<int>().Should().Be(1);

        await using var scope2 = factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        var after = await db2.RuleTestCases.CountAsync();
        (after - before).Should().Be(1);
    }

    [Fact]
    public async Task Run_from_blocks_endpoint()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var blocks = new JsonArray { new JsonObject { ["type"]="set", ["field"]="a", ["target"]="b" } };
        var resp = await client.PostAsJsonAsync("/api/v1/rules/properties/run-from-blocks", new { blocks, trials = 1 });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PropertyRunResult>();
        result!.Trials.Should().Be(1);
    }
}
