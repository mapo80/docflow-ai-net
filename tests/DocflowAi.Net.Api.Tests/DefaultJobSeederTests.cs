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
    public async Task Seeds_Default_Jobs()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetFromJsonAsync<JobListResponse>("/api/v1/jobs");

        resp!.total.Should().Be(3);
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("55555555-5555-5555-5555-555555555555"));

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/33333333-3333-3333-3333-333333333333");
        ok.GetProperty("model").GetString().Should().NotBeNullOrWhiteSpace();
        ok.GetProperty("templateToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Seeded_Job_Exposes_Artifacts()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/33333333-3333-3333-3333-333333333333");
        ok.GetProperty("paths").GetProperty("input").GetProperty("path").GetString()
            .Should().EndWith("/input.pdf");
        ok.GetProperty("paths").GetProperty("output").GetProperty("path").GetString()
            .Should().EndWith("/output.json");
        ok.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString()
            .Should().EndWith("/layout.json");
        ok.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString()
            .Should().EndWith("/output-layout.json");
        ok.GetProperty("paths").GetProperty("error").ValueKind
            .Should().Be(JsonValueKind.Null);

        var fileResp = await client.GetAsync(ok.GetProperty("paths").GetProperty("input").GetProperty("path").GetString());
        fileResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var mdResp = await client.GetAsync(ok.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString());
        mdResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loResp = await client.GetAsync(ok.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString());
        loResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var img = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/55555555-5555-5555-5555-555555555555");
        img.GetProperty("paths").GetProperty("input").GetProperty("path").GetString()
            .Should().EndWith("/input.png");
        img.GetProperty("paths").GetProperty("output").GetProperty("path").GetString()
            .Should().EndWith("/output.json");
        img.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString()
            .Should().EndWith("/layout.json");
        img.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString()
            .Should().EndWith("/output-layout.json");
        img.GetProperty("paths").GetProperty("error").ValueKind
            .Should().Be(JsonValueKind.Null);

        var imgFileResp = await client.GetAsync(img.GetProperty("paths").GetProperty("input").GetProperty("path").GetString());
        imgFileResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var imgMdResp = await client.GetAsync(img.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString());
        imgMdResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var imgLoResp = await client.GetAsync(img.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString());
        imgLoResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeded_Error_Job_Exposes_Error()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var fail = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/44444444-4444-4444-4444-444444444444");
        fail.GetProperty("status").GetString().Should().Be("Failed");
        fail.GetProperty("errorMessage").GetString().Should().Be("Mock extraction error");
        var paths = fail.GetProperty("paths");
        paths.GetProperty("error").GetProperty("path").GetString()
            .Should().EndWith("/error.txt");
        paths.GetProperty("output").ValueKind.Should().Be(JsonValueKind.Null);
        var errResp = await client.GetAsync(paths.GetProperty("error").GetProperty("path").GetString());
        errResp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await errResp.Content.ReadAsStringAsync()).Should().Contain("Mock extraction error");
    }

    [Fact]
    public async Task Seeded_Job_Copies_Source_Files()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();

        var datasetRoot = FindDatasetRoot();

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/33333333-3333-3333-3333-333333333333");
        var okInput = ok.GetProperty("paths").GetProperty("input").GetProperty("path").GetString()!;
        var okOutput = ok.GetProperty("paths").GetProperty("output").GetProperty("path").GetString()!;
        var okLayout = ok.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString()!;
        var okLayoutOutput = ok.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString()!;
        (await client.GetByteArrayAsync(okInput))
            .Should().BeEquivalentTo(File.ReadAllBytes(Path.Combine(datasetRoot, "job-seed", "input.pdf")));
        (await client.GetStringAsync(okOutput))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed", "output.json")));
        (await client.GetStringAsync(okLayout))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed", "layout.json")));
        (await client.GetStringAsync(okLayoutOutput))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed", "output-layout.json")));

        var img = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/55555555-5555-5555-5555-555555555555");
        var imgInput = img.GetProperty("paths").GetProperty("input").GetProperty("path").GetString()!;
        var imgOutput = img.GetProperty("paths").GetProperty("output").GetProperty("path").GetString()!;
        var imgLayout = img.GetProperty("paths").GetProperty("layout").GetProperty("path").GetString()!;
        var imgLayoutOutput = img.GetProperty("paths").GetProperty("layoutOutput").GetProperty("path").GetString()!;
        (await client.GetByteArrayAsync(imgInput))
            .Should().BeEquivalentTo(File.ReadAllBytes(Path.Combine(datasetRoot, "job-seed-png", "input.png")));
        (await client.GetStringAsync(imgOutput))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed-png", "output.json")));
        (await client.GetStringAsync(imgLayout))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed-png", "layout.json")));
        (await client.GetStringAsync(imgLayoutOutput))
            .Should().Be(File.ReadAllText(Path.Combine(datasetRoot, "job-seed-png", "output-layout.json")));
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

        var ok = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/33333333-3333-3333-3333-333333333333");
        var okPaths = ok.GetProperty("paths");
        await AssertBinaryFile(client, okPaths.GetProperty("input").GetProperty("path").GetString()!, new byte[] { 0x25, 0x50, 0x44, 0x46 });
        await AssertJsonFile(client, okPaths.GetProperty("output").GetProperty("path").GetString()!);
        await AssertTextFile(client, okPaths.GetProperty("markdown").GetProperty("path").GetString()!, "text/markdown");
        await AssertJsonFile(client, okPaths.GetProperty("layout").GetProperty("path").GetString()!);
        await AssertJsonFile(client, okPaths.GetProperty("layoutOutput").GetProperty("path").GetString()!);

        var img = await client.GetFromJsonAsync<JsonElement>("/api/v1/jobs/55555555-5555-5555-5555-555555555555");
        var imgPaths = img.GetProperty("paths");
        await AssertBinaryFile(client, imgPaths.GetProperty("input").GetProperty("path").GetString()!, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await AssertJsonFile(client, imgPaths.GetProperty("output").GetProperty("path").GetString()!);
        await AssertTextFile(client, imgPaths.GetProperty("markdown").GetProperty("path").GetString()!, "text/markdown");
        await AssertJsonFile(client, imgPaths.GetProperty("layout").GetProperty("path").GetString()!);
        await AssertJsonFile(client, imgPaths.GetProperty("layoutOutput").GetProperty("path").GetString()!);

        static async Task AssertBinaryFile(HttpClient client, string path, byte[] signature)
        {
            var resp = await client.GetAsync(path);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            bytes.Length.Should().BeGreaterThan(signature.Length);
            bytes.Take(signature.Length).Should().Equal(signature);
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

        static async Task AssertTextFile(HttpClient client, string path, string contentType)
        {
            var resp = await client.GetAsync(path);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be(contentType);
            var text = await resp.Content.ReadAsStringAsync();
            text.Should().NotBeNullOrWhiteSpace();
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
    private record JobItem(Guid id, string status, string derivedStatus, int progress, DateTimeOffset createdAt, DateTimeOffset updatedAt, string language);
}
