using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","RuleTestsEndpoints")]
public class RuleTestsEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RuleTestsEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Full_flow()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var ruleResp = await client.PostAsJsonAsync("/api/v1/rules", new { name = "r1", code = "set(\"x\",1);", readsCsv = (string?)null, writesCsv = (string?)null, enabled = true });
        ruleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await ruleResp.Content.ReadFromJsonAsync<JsonObject>();
        var ruleId = Guid.Parse(rule!["id"]!.GetValue<string>());

        var expect1 = new JsonObject { ["fields"] = new JsonObject { ["x"] = 1 } };
        var expect2 = new JsonObject { ["fields"] = new JsonObject { ["x"] = 2 } };
        var t1Resp = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/tests", new { name = "t1", input = new JsonObject(), expect = expect1, suite = (string?)null, tags = (string[]?)null, priority = 1 });
        t1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var t1 = await t1Resp.Content.ReadFromJsonAsync<JsonObject>();
        var t1Id = Guid.Parse(t1!["id"]!.GetValue<string>());
        var t2Resp = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/tests", new { name = "t2", input = new JsonObject(), expect = expect2, suite = (string?)null, tags = (string[]?)null, priority = 1 });
        t2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var t2 = await t2Resp.Content.ReadFromJsonAsync<JsonObject>();
        var t2Id = Guid.Parse(t2!["id"]!.GetValue<string>());

        var listResp = await client.GetAsync($"/api/v1/rules/{ruleId}/tests");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listObj = await listResp.Content.ReadFromJsonAsync<JsonObject>();
        listObj!["total"]!.GetValue<int>().Should().Be(2);

        var upd = await client.PutAsJsonAsync($"/api/v1/rules/{ruleId}/tests/{t2Id}", new { name = "t2-upd", suite = "s", tags = new[] { "a" }, priority = 2 });
        upd.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cloneResp = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/tests/{t1Id}/clone", new { newName = "copy", suite = (string?)null, tags = (string[]?)null });
        cloneResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var runAll = await client.PostAsync($"/api/v1/rules/{ruleId}/tests/run", null);
        runAll.StatusCode.Should().Be(HttpStatusCode.OK);
        var runArr = await runAll.Content.ReadFromJsonAsync<JsonArray>();
        runArr!.Count.Should().Be(3);

        var runSel = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/tests/run-selected", new { ids = new[] { t1Id } });
        runSel.StatusCode.Should().Be(HttpStatusCode.OK);
        var runSelArr = await runSel.Content.ReadFromJsonAsync<JsonArray>();
        var runSelId = runSelArr?.Single()? ["id"]?.GetValue<Guid>();
        runSelId.Should().Be((Guid?)t1Id);

        var covResp = await client.GetAsync($"/api/v1/rules/{ruleId}/tests/coverage");
        covResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cov = await covResp.Content.ReadFromJsonAsync<JsonArray>();
        cov![0]!["field"]!.GetValue<string>().Should().Be("x");
    }

    [Fact]
    public async Task Run_missing_rule_returns_not_found()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PostAsync($"/api/v1/rules/{Guid.NewGuid()}/tests/run", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

