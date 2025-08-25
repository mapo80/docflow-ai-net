using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Data;
using Microsoft.Extensions.DependencyInjection;
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

        var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = base64, fileName = "input.pdf", model = "m", templateToken = "t", language = "eng", engine = "tesseract" });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("job_id").GetGuid();

        var dir = Path.Combine(factory.DataRootPath, id.ToString("N"));
        Directory.Exists(dir).Should().BeTrue();
        File.Exists(Path.Combine(dir, "input.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "prompt.txt")).Should().BeFalse();
        File.Exists(Path.Combine(dir, "fields.json")).Should().BeFalse();
        File.Exists(Path.Combine(dir, "manifest.json")).Should().BeTrue();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db, id);
        job.Should().NotBeNull();
        job!.Status.Should().BeOneOf("Queued","Succeeded","Running");
        job.Language.Should().Be("eng");
        job.Engine.Should().Be(DocflowAi.Net.Application.Markdown.OcrEngine.Tesseract);
    }
}
