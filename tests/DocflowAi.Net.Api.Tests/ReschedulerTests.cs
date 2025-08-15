using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Hosted;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class ReschedulerTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ReschedulerTests(TempDirFixture fx) => _fx = fx;

    private static JobDocument CreateRunning(Guid id, string dir, string input)
        => new()
        {
            Id = id,
            Status = "Running",
            Progress = 0,
            Attempts = 0,
            LeaseUntil = DateTimeOffset.UtcNow.AddSeconds(-1),
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = input,
                Output = Path.Combine(dir, "output.json"),
                Error = Path.Combine(dir, "error.txt")
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Hash = "h",
            Metrics = new JobDocument.MetricsInfo()
        };

    [Fact]
    public async Task Requeues_Expired_Lease()
    {
        await using var factory = new TestWebAppFactory_Step3B(_fx.RootPath);
        var store = factory.GetService<IJobRepository>();
        var uow = factory.GetService<IUnitOfWork>();
        var fs = factory.GetService<IFileSystemService>();
        var rescheduler = factory.GetService<IEnumerable<IHostedService>>().OfType<ReschedulerService>().Single();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateRunning(id, dir, input));
        uow.SaveChanges();

        await rescheduler.ProcessOnceAsync(CancellationToken.None);
        using var verifyScope = factory.Services.CreateScope();
        var job = verifyScope.ServiceProvider.GetRequiredService<IJobRepository>().Get(id)!;
        job.Status.Should().Be("Queued");
        job.Attempts.Should().Be(1);
        job.AvailableAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Marks_Failed_On_Max_Attempts()
    {
        await using var factory = new TestWebAppFactory_Step3B(_fx.RootPath);
        var store = factory.GetService<IJobRepository>();
        var uow2 = factory.GetService<IUnitOfWork>();
        var fs = factory.GetService<IFileSystemService>();
        var rescheduler = factory.GetService<IEnumerable<IHostedService>>().OfType<ReschedulerService>().Single();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        var doc = CreateRunning(id, dir, input);
        doc.Attempts = 2; // max attempts
        store.Create(doc);
        uow2.SaveChanges();

        await rescheduler.ProcessOnceAsync(CancellationToken.None);
        using var verifyScope = factory.Services.CreateScope();
        var job = verifyScope.ServiceProvider.GetRequiredService<IJobRepository>().Get(id)!;
        job.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Contain("max attempts");
        File.Exists(Path.Combine(dir, "error.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task Requeued_Job_Can_Run_To_Success()
    {
        await using var factory = new TestWebAppFactory_Step3B(_fx.RootPath);
        var store = factory.GetService<IJobRepository>();
        var uow3 = factory.GetService<IUnitOfWork>();
        var fs = factory.GetService<IFileSystemService>();
        var rescheduler = factory.GetService<IEnumerable<IHostedService>>().OfType<ReschedulerService>().Single();
        var runner = factory.GetService<IJobRunner>();
        factory.Fake.CurrentMode = FakeProcessService.Mode.Success;

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateRunning(id, dir, input));
        uow3.SaveChanges();

        await rescheduler.ProcessOnceAsync(CancellationToken.None);
        // make available immediately
        store.Requeue(id, 1, DateTimeOffset.UtcNow.AddMilliseconds(-1));
        uow3.SaveChanges();

        await runner.Run(id, CancellationToken.None);
        var job = store.Get(id)!;
        job.Status.Should().Be("Succeeded");
        File.Exists(Path.Combine(dir, "output.json")).Should().BeTrue();
    }
}
