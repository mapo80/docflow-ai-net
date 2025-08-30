using System.Net.Http.Json;
using System.Text.Json;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","AiTestsEndpoints")]
public class AiTestsEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public AiTestsEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Suggest_and_import_flow()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            var rule = new RuleFunction { Name = "r", Code = "var r=new Regex(\"[0-9]+\"); if(amount>10){ }", CodeHash = "h" };
            db.RuleFunctions.Add(rule);
            await db.SaveChangesAsync();
        }

        Guid ruleId;
        int before;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            ruleId = db.RuleFunctions.First().Id;
            before = db.RuleTestCases.Count();
        }

        var suggestResp = await client.PostAsJsonAsync($"/api/v1/ai/tests/suggest?ruleId={ruleId}", new { });
        suggestResp.EnsureSuccessStatusCode();
        var suggest = await suggestResp.Content.ReadFromJsonAsync<JsonElement>();
        var ids = suggest.GetProperty("suggestions").EnumerateArray().Select(j => j.GetProperty("id").GetGuid()).ToArray();
        ids.Length.Should().BeGreaterThan(0);

        var importResp = await client.PostAsJsonAsync($"/api/v1/ai/tests/import?ruleId={ruleId}", new { ids });
        importResp.EnsureSuccessStatusCode();
        var import = await importResp.Content.ReadFromJsonAsync<JsonElement>();
        import.GetProperty("imported").GetInt32().Should().Be(ids.Length);

        await using var scope2 = factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        db2.RuleTestCases.Count().Should().Be(before + ids.Length);
    }
}
