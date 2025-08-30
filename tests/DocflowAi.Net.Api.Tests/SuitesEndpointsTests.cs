using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","SuitesEndpoints")]
public class SuitesEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public SuitesEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Crud_and_clone_flow()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var create = await client.PostAsJsonAsync("/api/v1/suites", new { name = "a", color = "red", description = "d" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var suite = await create.Content.ReadFromJsonAsync<JsonObject>();
        var id = Guid.Parse(suite!["id"]!.GetValue<string>());

        var list = await client.GetFromJsonAsync<JsonObject>("/api/v1/suites");
        list!["items"]!.AsArray().Count.Should().Be(1);

        var put = await client.PutAsJsonAsync($"/api/v1/suites/{id}", new { name = "b", color = "blue", description = "u" });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var clone = await client.PostAsJsonAsync($"/api/v1/suites/{id}/clone", new { newName = "c" });
        clone.StatusCode.Should().Be(HttpStatusCode.OK);
        var cobj = await clone.Content.ReadFromJsonAsync<JsonObject>();
        cobj!["name"]!.GetValue<string>().Should().Be("c");

        var del = await client.DeleteAsync($"/api/v1/suites/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var final = await client.GetFromJsonAsync<JsonObject>("/api/v1/suites");
        final!["items"]!.AsArray().Count.Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_name_conflict()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        await client.PostAsJsonAsync("/api/v1/suites", new { name = "dup" });
        var resp = await client.PostAsJsonAsync("/api/v1/suites", new { name = "dup" });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_missing_returns_not_found()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PutAsJsonAsync($"/api/v1/suites/{Guid.NewGuid()}", new { name = "x" });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Clone_conflict_and_missing()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var create = await client.PostAsJsonAsync("/api/v1/suites", new { name = "s1" });
        var suite = await create.Content.ReadFromJsonAsync<JsonObject>();
        var id = Guid.Parse(suite!["id"]!.GetValue<string>());

        var nf = await client.PostAsJsonAsync($"/api/v1/suites/{Guid.NewGuid()}/clone", new { newName = "x" });
        nf.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await client.PostAsJsonAsync("/api/v1/suites", new { name = "s2" });
        var conflict = await client.PostAsJsonAsync($"/api/v1/suites/{id}/clone", new { newName = "s2" });
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

