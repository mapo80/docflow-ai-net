using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Http;
using DocflowAi.Net.Api.Tests.Fixtures;
using System.Linq;
using System.Text.Json;
using System.Net;
using System.IO;

namespace DocflowAi.Net.Api.Tests;

public class DefaultJobSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;

    public DefaultJobSeederTests(TempDirFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seeds_Two_Default_Jobs()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetFromJsonAsync<JobListResponse>("/api/v1/jobs");

        resp!.total.Should().Be(2);
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public async Task Seeded_Jobs_Expose_Artifacts()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/11111111-1111-1111-1111-111111111111");
        ok.GetProperty("paths").GetProperty("input").GetString()
            .Should().EndWith("/input.pdf");
        ok.GetProperty("paths").GetProperty("output").GetString()
            .Should().EndWith("/output.json");
        ok.GetProperty("paths").GetProperty("error").ValueKind
            .Should().Be(JsonValueKind.Null);

        var err = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/22222222-2222-2222-2222-222222222222");
        err.GetProperty("paths").GetProperty("input").GetString()
            .Should().EndWith("/input.png");
        err.GetProperty("paths").GetProperty("output").ValueKind
            .Should().Be(JsonValueKind.Null);
        err.GetProperty("paths").GetProperty("error").GetString()
            .Should().EndWith("/error.txt");

        var fileResp = await client.GetAsync(ok.GetProperty("paths").GetProperty("input").GetString());
        fileResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeded_Jobs_Copy_Source_Files()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var datasetRoot = FindDatasetRoot();

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/11111111-1111-1111-1111-111111111111");
        var okInput = ok.GetProperty("paths").GetProperty("input").GetString()!;
        var okPrompt = ok.GetProperty("paths").GetProperty("prompt").GetString()!;
        var okFields = ok.GetProperty("paths").GetProperty("fields").GetString()!;
        var okOutput = ok.GetProperty("paths").GetProperty("output").GetString()!;
        (await client.GetByteArrayAsync(okInput))
            .Should().BeEquivalentTo(File.ReadAllBytes(Path.Combine(datasetRoot, "sample_invoice.pdf")));
        (await client.GetStringAsync(okPrompt))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-pdf", "prompt.txt")));
        (await client.GetStringAsync(okFields))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-pdf", "fields.txt")));
        (await client.GetStringAsync(okOutput))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-png-boxsolver-pointerstrategy", "result.json")));

        var err = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/22222222-2222-2222-2222-222222222222");
        var errInput = err.GetProperty("paths").GetProperty("input").GetString()!;
        var errPrompt = err.GetProperty("paths").GetProperty("prompt").GetString()!;
        var errFields = err.GetProperty("paths").GetProperty("fields").GetString()!;
        var errError = err.GetProperty("paths").GetProperty("error").GetString()!;
        (await client.GetByteArrayAsync(errInput))
            .Should().BeEquivalentTo(File.ReadAllBytes(Path.Combine(datasetRoot, "sample_invoice.png")));
        (await client.GetStringAsync(errPrompt))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-png", "prompt.txt")));
        (await client.GetStringAsync(errFields))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-png", "fields.txt")));
        (await client.GetStringAsync(errError))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "test-png", "llm_response.txt")));
    }

    [Fact]
    public async Task Seeded_Job_Files_Return_Correct_Content_Types()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/11111111-1111-1111-1111-111111111111");
        var okPaths = ok.GetProperty("paths");
        await AssertBinaryFile(client, okPaths.GetProperty("input").GetString()!, new byte[] { 0x25, 0x50, 0x44, 0x46 });
        await AssertTextFile(client, okPaths.GetProperty("prompt").GetString()!);
        await AssertTextFile(client, okPaths.GetProperty("fields").GetString()!);
        await AssertJsonFile(client, okPaths.GetProperty("output").GetString()!);

        var err = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/22222222-2222-2222-2222-222222222222");
        var errPaths = err.GetProperty("paths");
        await AssertBinaryFile(client, errPaths.GetProperty("input").GetString()!, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        await AssertTextFile(client, errPaths.GetProperty("prompt").GetString()!);
        await AssertTextFile(client, errPaths.GetProperty("fields").GetString()!);
        await AssertTextFile(client, errPaths.GetProperty("error").GetString()!);

        static async Task AssertBinaryFile(HttpClient client, string path, byte[] signature)
        {
            var resp = await client.GetAsync(path);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            bytes.Length.Should().BeGreaterThan(signature.Length);
            bytes.Take(signature.Length).Should().Equal(signature);
        }

        static async Task AssertTextFile(HttpClient client, string path)
        {
            var resp = await client.GetAsync(path);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
            var text = await resp.Content.ReadAsStringAsync();
            text.Should().NotBeNullOrWhiteSpace();
        }

        static async Task AssertJsonFile(HttpClient client, string path)
        {
            var resp = await client.GetAsync(path);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            var text = await resp.Content.ReadAsStringAsync();
            text.Should().NotBeNullOrWhiteSpace();
            JsonDocument.Parse(text);
        }
    }

    private static string FindDatasetRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "dataset");
            if (Directory.Exists(path))
                return path;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("dataset");
    }

    private record JobListResponse(int page, int pageSize, int total, List<JobItem> items);
    private record JobItem(Guid id, string status, string derivedStatus, int progress, DateTimeOffset createdAt, DateTimeOffset updatedAt);
}
