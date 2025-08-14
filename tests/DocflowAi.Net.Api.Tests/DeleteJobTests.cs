using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace DocflowAi.Net.Api.Tests;

public class DeleteJobTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public DeleteJobTests(TempDirFixture fx) => _fx = fx;

    [Theory]
    [InlineData("Queued")]
    [InlineData("Running")]
    public async Task Delete_pending_or_running_marks_cancelled_and_logs(string status)
    {
        var id = Guid.NewGuid();
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<IJobStore>();
        var job = LiteDbTestHelper.CreateJob(id, status, DateTimeOffset.UtcNow);
        store.Create(job);
        var resp = await client.DeleteAsync($"/v1/jobs/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var updated = LiteDbTestHelper.GetJob(factory.LiteDbPath, id);
        updated!.Status.Should().Be("Cancelled");
    }

    [Theory]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    public async Task Delete_terminal_returns_conflict_and_logs(string status)
    {
        var id = Guid.NewGuid();
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<IJobStore>();
        var job = LiteDbTestHelper.CreateJob(id, status, DateTimeOffset.UtcNow);
        store.Create(job);
        var resp = await client.DeleteAsync($"/v1/jobs/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
