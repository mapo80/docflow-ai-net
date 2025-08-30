using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","RulesEndpoints")]
public class RulesEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RulesEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Crud_and_execution_flow()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var create = await client.PostAsJsonAsync("/api/v1/rules", new { name = "r1", description = "d", code = "set(\"x\",1);", readsCsv = (string?)null, writesCsv = (string?)null, enabled = true });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var obj = await create.Content.ReadFromJsonAsync<JsonObject>();
        var id = Guid.Parse(obj!["id"]!.GetValue<string>());

        var listResp = await client.GetAsync("/api/v1/rules");
        var listBody = await listResp.Content.ReadAsStringAsync();
        listResp.StatusCode.Should().Be(HttpStatusCode.OK, listBody);
        var list = JsonNode.Parse(listBody)!.AsObject();
        var initialTotal = list!["total"]!.GetValue<int>();
        list["items"]!.AsArray().Any(n => n!["name"]!.GetValue<string>() == "r1").Should().BeTrue();

        var get = await client.GetFromJsonAsync<JsonObject>($"/api/v1/rules/{id}");
        get!["name"]!.GetValue<string>().Should().Be("r1");

        var put = await client.PutAsJsonAsync($"/api/v1/rules/{id}", new { name = "r2", description = "u", code = "set(\"x\",2);", readsCsv = (string?)null, writesCsv = (string?)null, enabled = true });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.PostAsync($"/api/v1/rules/{id}/stage", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/rules/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var comp = await client.PostAsync($"/api/v1/rules/{id}/compile", null);
        comp.StatusCode.Should().Be(HttpStatusCode.OK);

        var run = await client.PostAsJsonAsync($"/api/v1/rules/{id}/run", new { input = new JsonObject() });
        run.StatusCode.Should().Be(HttpStatusCode.OK);

        var cloneResp = await client.PostAsJsonAsync($"/api/v1/rules/{id}/clone", new { newName = "copy", includeTests = false });
        cloneResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var clone = await cloneResp.Content.ReadFromJsonAsync<JsonObject>();
        Guid.Parse(clone!["id"]!.GetValue<string>()).Should().NotBe(id);

        var afterResp = await client.GetAsync("/api/v1/rules");
        var afterBody = await afterResp.Content.ReadAsStringAsync();
        afterResp.StatusCode.Should().Be(HttpStatusCode.OK, afterBody);
        var after = JsonNode.Parse(afterBody)!.AsObject();
        after!["total"]!.GetValue<int>().Should().Be(initialTotal + 1); // clone adds one
    }

    [Fact]
    public async Task Duplicate_name_conflict()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        await client.PostAsJsonAsync("/api/v1/rules", new { name = "dup", code = "//code", readsCsv = (string?)null, writesCsv = (string?)null, enabled = true });
        var resp = await client.PostAsJsonAsync("/api/v1/rules", new { name = "dup", code = "//code", readsCsv = (string?)null, writesCsv = (string?)null, enabled = true });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Run_missing_rule_returns_not_found()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PostAsJsonAsync($"/api/v1/rules/{Guid.NewGuid()}/run", new { input = new JsonObject() });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

