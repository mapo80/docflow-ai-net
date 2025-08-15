using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Data;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateJobSuccessTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateJobSuccessTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Immediate_Success_Returns200_And_WritesOutput_SetsSucceeded()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Success;
        using var ctx = TestCorrelator.CreateContext();
        var client = factory.CreateClient();
        var pdf = new byte[]{1,2,3,4};
        var payload = new { fileBase64 = Convert.ToBase64String(pdf), fileName = "a.pdf" };
        var res = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", payload);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("job_id").GetGuid();
        body.GetProperty("status").GetString().Should().Be("Succeeded");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db, id)!;
        job.Status.Should().Be("Succeeded");
        job.Progress.Should().Be(100);
        job.Metrics.EndedAt.Should().NotBeNull();
        var dir = Path.Combine(factory.DataRootPath, id.ToString("N"));
        File.Exists(Path.Combine(dir, "output.json")).Should().BeTrue();
        Directory.EnumerateFiles(dir, "*.tmp").Should().BeEmpty();
    }
}
