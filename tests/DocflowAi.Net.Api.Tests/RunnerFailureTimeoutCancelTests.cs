using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class RunnerFailureTimeoutCancelTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RunnerFailureTimeoutCancelTests(TempDirFixture fx) => _fx = fx;

    private static JobDocument CreateDoc(Guid id, string dir, string input, string dataRoot)
        => new()
        {
            Id = id,
            Status = "Queued",
            Progress = 0,
            Attempts = 0,
            AvailableAt = DateTimeOffset.UtcNow,
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = input,
                Output = PathHelpers.OutputPath(dataRoot, id),
                Error = PathHelpers.ErrorPath(dataRoot, id),
                Markdown = PathHelpers.MarkdownPath(dataRoot, id)
            }
        };

    [Fact]
    public async Task RunAsync_Fail_WritesError_And_SetsFailed()
    {
        await using var factory = new TestWebAppFactory_Step3A(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Fail;
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var fs = sp.GetRequiredService<IFileSystemService>();
        var store = sp.GetRequiredService<IJobRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input, factory.DataRootPath));
        uow.SaveChanges();

        await runner.Run(id, CancellationToken.None);

        using var scope2 = factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db, id)!;
        job.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Contain("boom");
        File.Exists(PathHelpers.OutputPath(factory.DataRootPath, id)).Should().BeFalse();
        File.ReadAllText(PathHelpers.ErrorPath(factory.DataRootPath, id)).Should().Contain("boom");
    }

    [Fact]
    public async Task RunAsync_Timeout_WritesError_And_SetsFailed()
    {
        await using var factory = new TestWebAppFactory_Step3A(_fx.RootPath, timeoutSeconds:2);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Slow;
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var fs = sp.GetRequiredService<IFileSystemService>();
        var store = sp.GetRequiredService<IJobRepository>();
        var uow2 = sp.GetRequiredService<IUnitOfWork>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input, factory.DataRootPath));
        uow2.SaveChanges();

        await runner.Run(id, CancellationToken.None);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db2, id)!;
        job.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Contain("timeout");
        File.ReadAllText(PathHelpers.ErrorPath(factory.DataRootPath, id)).Should().Contain("timeout");
    }

    [Fact]
    public async Task RunAsync_Cancel_WritesError_And_SetsCancelled()
    {
        await using var factory = new TestWebAppFactory_Step3A(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Cancellable;
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var fs = sp.GetRequiredService<IFileSystemService>();
        var store = sp.GetRequiredService<IJobRepository>();
        var uow3 = sp.GetRequiredService<IUnitOfWork>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input, factory.DataRootPath));
        uow3.SaveChanges();

        using var cts = new CancellationTokenSource();
        var runTask = runner.Run(id, cts.Token);
        cts.CancelAfter(100);
        await runTask;

        using var scope3 = factory.Services.CreateScope();
        var db3 = scope3.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db3, id)!;
        job.Status.Should().Be("Cancelled");
        File.ReadAllText(PathHelpers.ErrorPath(factory.DataRootPath, id)).Should().Contain("cancelled");
    }
}
