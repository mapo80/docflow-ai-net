using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocflowAi.Net.Api.Tests;

public class IdempotencyAndDedupeTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public IdempotencyAndDedupeTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task IdempotencyKey_returns_same_job()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[16];
        new Random(1).NextBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = JsonContent.Create(new { fileBase64 = base64, fileName = "a.pdf", model = "m", templateToken = "t" }) };
        req1.Headers.Add("Idempotency-Key", "K1");
        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var id1 = (await resp1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs") { Content = JsonContent.Create(new { fileBase64 = base64, fileName = "a.pdf", model = "m", templateToken = "t" }) };
        req2.Headers.Add("Idempotency-Key", "K1");
        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var id2 = (await resp2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();
        id2.Should().Be(id1);
        Directory.GetDirectories(factory.DataRootPath).Length.Should().Be(1);
    }

    [Fact]
    public async Task Hash_dedupe_returns_existing_job()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[32];
        new Random(2).NextBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);

        var resp1 = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = base64, fileName = "b.pdf", model = "m", templateToken = "t" });
        resp1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var id1 = (await resp1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();

        var resp2 = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = base64, fileName = "b.pdf", model = "m", templateToken = "t" });
        resp2.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var id2 = (await resp2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();
        id2.Should().Be(id1);
        Directory.GetDirectories(factory.DataRootPath).Length.Should().Be(1);
    }

    [Fact]
    public async Task IdempotencyKey_returns_same_job_immediate()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[16];
        new Random(3).NextBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs?mode=immediate") { Content = JsonContent.Create(new { fileBase64 = base64, fileName = "c.pdf", model = "m", templateToken = "t" }) };
        req1.Headers.Add("Idempotency-Key", "K2");
        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var id1 = (await resp1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs?mode=immediate") { Content = JsonContent.Create(new { fileBase64 = base64, fileName = "c.pdf", model = "m", templateToken = "t" }) };
        req2.Headers.Add("Idempotency-Key", "K2");
        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var id2 = (await resp2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();
        id2.Should().Be(id1);
    }

    [Fact]
    public async Task Hash_dedupe_returns_existing_job_immediate()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[32];
        new Random(4).NextBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);

        var resp1 = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", new { fileBase64 = base64, fileName = "d.pdf", model = "m", templateToken = "t" });
        resp1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var id1 = (await resp1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();

        var resp2 = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", new { fileBase64 = base64, fileName = "d.pdf", model = "m", templateToken = "t" });
        resp2.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var id2 = (await resp2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("job_id").GetGuid();
        id2.Should().Be(id1);
    }
}
