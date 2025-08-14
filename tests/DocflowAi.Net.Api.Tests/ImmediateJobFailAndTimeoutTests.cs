using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateJobFailAndTimeoutTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateJobFailAndTimeoutTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Immediate_Fail_Returns200_StatusFailed_WritesError()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Fail;
        var client = factory.CreateClient();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf" };
        var res = await client.PostAsJsonAsync("/v1/jobs?mode=immediate", payload);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("job_id").GetGuid();
        body.GetProperty("status").GetString().Should().Be("Failed");
        var job = LiteDbTestHelper.GetJob(factory.LiteDbPath, id)!;
        job.Status.Should().Be("Failed");
        File.Exists(Path.Combine(factory.DataRootPath, id.ToString("N"), "error.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task Immediate_Timeout_Returns200_StatusFailed_WritesTimeout()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath, timeoutSeconds:1);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Slow;
        var client = factory.CreateClient();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf" };
        var res = await client.PostAsJsonAsync("/v1/jobs?mode=immediate", payload);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("job_id").GetGuid();
        body.GetProperty("status").GetString().Should().Be("Failed");
        var job = LiteDbTestHelper.GetJob(factory.LiteDbPath, id)!;
        job.Status.Should().Be("Failed");
        File.ReadAllText(Path.Combine(factory.DataRootPath, id.ToString("N"), "error.txt")).Should().Contain("timeout");
    }
}
