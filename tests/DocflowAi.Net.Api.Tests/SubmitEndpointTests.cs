using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DocflowAi.Net.Api.Tests;

public class SubmitEndpointTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public SubmitEndpointTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Submit_creates_directory_files_and_db_record()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var bytes = new byte[4096];
        new Random(1).NextBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);

        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = base64, fileName = "input.pdf", prompt = "hi", fields = "{}" });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("job_id").GetGuid();

        var dir = Path.Combine(factory.DataRootPath, id.ToString("N"));
        Directory.Exists(dir).Should().BeTrue();
        File.Exists(Path.Combine(dir, "input.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "prompt.txt")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "fields.json")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "manifest.json")).Should().BeTrue();

        var job = LiteDbTestHelper.GetJob(factory.LiteDbPath, id);
        job.Should().NotBeNull();
        job!.Status.Should().BeOneOf("Queued","Succeeded","Running");
    }
}
