using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","BuiltinIbanRule")]
public class BuiltinIbanRuleTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public BuiltinIbanRuleTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Builtin_rule_normalizes_and_validates_iban()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);

        var listResp = await client.GetAsync("/api/v1/rules?search=Builtins.Iban.NormalizeAndValidate");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listObj = await listResp.Content.ReadFromJsonAsync<JsonObject>();
        var ruleId = listObj!["items"]!.AsArray().First()!["id"]!.GetValue<Guid>();

        var runResp = await client.PostAsJsonAsync($"/api/v1/rules/{ruleId}/run",
            new { input = new { fields = new { ibanRaw = new { value = "IT60 X054 2811 1010 0000 0123 456" } } } });
        runResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var run = await runResp.Content.ReadFromJsonAsync<JsonObject>();
        run!["after"]!["iban"]!.GetValue<string>().Should().Be("IT60X0542811101000000123456");

        var testResp = await client.PostAsync($"/api/v1/rules/{ruleId}/tests/run", null);
        testResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tests = await testResp.Content.ReadFromJsonAsync<JsonArray>();
        tests![0]! ["passed"]!.GetValue<bool>().Should().BeTrue();
    }
}
