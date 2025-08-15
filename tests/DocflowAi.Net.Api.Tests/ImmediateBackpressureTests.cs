using System.Net.Http.Json;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateBackpressureTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateBackpressureTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Immediate_Respects_GlobalBackpressure_429_NoSideEffects()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath, maxQueueLength: 1);
        var client = factory.CreateClient();
        var existing = DbTestHelper.CreateJob(Guid.NewGuid(), "Queued", DateTimeOffset.UtcNow);
        var store = factory.GetService<IJobRepository>();
        var uow = factory.GetService<IUnitOfWork>();
        store.Create(existing);
        uow.SaveChanges();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf" };
        using (TestCorrelator.CreateContext())
        {
            var resp = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", payload);
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
            resp.Headers.Should().ContainKey("Retry-After");
            (await resp.Content.ReadAsStringAsync()).Should().Contain("queue_full");
            Directory.GetDirectories(factory.DataRootPath).Should().BeEmpty();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            db.Jobs.Count().Should().Be(1);
            TestCorrelator.GetLogEventsFromCurrentContext();
        }
    }
}
