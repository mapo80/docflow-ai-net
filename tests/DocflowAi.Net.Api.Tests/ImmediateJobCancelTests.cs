using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Models;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateJobCancelTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateJobCancelTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Immediate_Cancel_RespectsRequestAborted_SetsCancelled()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Cancellable;
        var client = factory.CreateClient();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf" };
        using var cts = new CancellationTokenSource();
        using (TestCorrelator.CreateContext())
        {
            var post = client.PostAsJsonAsync("/v1/jobs?mode=immediate", payload, cts.Token);
            await Task.Delay(100);
            cts.Cancel();
            try { await post; } catch (TaskCanceledException) { }
            await Task.Delay(100); // allow runner to finish
            using var db = LiteDbTestHelper.Open(factory.LiteDbPath);
            var job = db.GetCollection<JobDocument>("jobs").FindAll().FirstOrDefault();
            if (job != null)
            {
                job.Status.Should().Be("Cancelled");
                File.Exists(PathHelpers.ErrorPath(factory.DataRootPath, job.Id)).Should().BeTrue();
            }
            TestCorrelator.GetLogEventsFromCurrentContext();
        }
    }
}
