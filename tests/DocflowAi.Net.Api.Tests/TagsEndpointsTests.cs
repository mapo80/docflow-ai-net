using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","TagsEndpoints")]
public class TagsEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public TagsEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Crud_flow_works()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var create = await client.PostAsJsonAsync("/api/v1/tags", new { name = "a", color = "red", description = "d" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var tag = await create.Content.ReadFromJsonAsync<JsonObject>();
        var id = Guid.Parse(tag!["id"]!.GetValue<string>());

        var list = await client.GetFromJsonAsync<JsonObject>("/api/v1/tags");
        list!["items"]!.AsArray().Count.Should().Be(1);

        var put = await client.PutAsJsonAsync($"/api/v1/tags/{id}", new { name = "b", color = "blue", description = "u" });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<JsonObject>("/api/v1/tags");
        after!["items"]!.AsArray()[0]!.AsObject()["name"]!.GetValue<string>().Should().Be("b");

        var del = await client.DeleteAsync($"/api/v1/tags/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var empty = await client.GetFromJsonAsync<JsonObject>("/api/v1/tags");
        empty!["items"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_name_conflict()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        await client.PostAsJsonAsync("/api/v1/tags", new { name = "dup" });
        var resp = await client.PostAsJsonAsync("/api/v1/tags", new { name = "dup" });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_missing_returns_not_found()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PutAsJsonAsync($"/api/v1/tags/{Guid.NewGuid()}", new { name = "x" });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
