using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.JobQueue.Jobs;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog.Sinks.TestCorrelator;
using FluentAssertions;
using System.IO;
using System.Threading;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class CleanupE2ETests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public CleanupE2ETests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Old_jobs_removed_from_db_and_fs()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var ttl = factory.Services.GetRequiredService<IOptions<JobQueueOptions>>().Value.JobTTLDays;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-(ttl + 1));
        var dir1 = Path.Combine(factory.DataRootPath, Guid.NewGuid().ToString("N"));
        var dir2 = Path.Combine(factory.DataRootPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        var old1 = DbTestHelper.CreateJob(Guid.NewGuid(), "Succeeded", cutoff); old1.Paths.Dir = dir1;
        var old2 = DbTestHelper.CreateJob(Guid.NewGuid(), "Failed", cutoff); old2.Paths.Dir = dir2;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        DbTestHelper.SeedJobs(db, new[] { old1, old2 });
        var svc = scope.ServiceProvider.GetRequiredService<CleanupJob>();
        using (TestCorrelator.CreateContext())
        {
            await svc.RunOnceAsync(CancellationToken.None);
            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
            DbTestHelper.GetJob(db2, old1.Id).Should().BeNull();
            DbTestHelper.GetJob(db2, old2.Id).Should().BeNull();
            Directory.Exists(dir1).Should().BeFalse();
            Directory.Exists(dir2).Should().BeFalse();
            var events = TestCorrelator.GetLogEventsFromCurrentContext();
            events.Should().NotBeNull();
        }
    }
}
