using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Data;
using Microsoft.Extensions.DependencyInjection;
using DocflowAi.Net.Api.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Serilog.Sinks.TestCorrelator;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class BackpressureE2ETests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public BackpressureE2ETests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Queue_full_returns_429_without_side_effects()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath, maxQueueLength:1);
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var job = DbTestHelper.CreateJob(Guid.NewGuid(), "Queued", DateTimeOffset.UtcNow);
        store.Create(job);
        uow.SaveChanges();
        var dirsBefore = Directory.GetDirectories(factory.DataRootPath).Length;
        using (TestCorrelator.CreateContext())
        {
            var resp = await client.PostAsJsonAsync("/api/v1/jobs", new { fileBase64 = Convert.ToBase64String(new byte[10]), fileName = "c.pdf", model = "m", templateToken = "t", language = "eng", engine = "tesseract" });
            resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            resp.Headers.Should().ContainKey("Retry-After");
            Directory.GetDirectories(factory.DataRootPath).Length.Should().Be(dirsBefore);
            using var scope2 = factory.Services.CreateScope();
            var db = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
            DbTestHelper.GetJob(db, job.Id).Should().NotBeNull();
            var events = TestCorrelator.GetLogEventsFromCurrentContext();
            events.Should().NotBeNull(); // placeholder to consume events
        }
    }
}
