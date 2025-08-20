using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class RunnerSuccessTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RunnerSuccessTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task RunAsync_Success_WritesOutput_And_SetsSucceeded()
    {
        await using var factory = new TestWebAppFactory_Step3A(_fx.RootPath);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Success;
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var fs = sp.GetRequiredService<IFileSystemService>();
        var store = sp.GetRequiredService<IJobRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var inputPath = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(inputPath)!;
        var doc = new JobDocument
        {
            Id = id,
            Status = "Queued",
            Progress = 0,
            Attempts = 0,
            AvailableAt = DateTimeOffset.UtcNow,
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = inputPath,
                Output = PathHelpers.OutputPath(factory.DataRootPath, id),
                Error = PathHelpers.ErrorPath(factory.DataRootPath, id),
                Markdown = PathHelpers.MarkdownPath(factory.DataRootPath, id)
            }
        };
        store.Create(doc);
        uow.SaveChanges();

        await runner.Run(id, CancellationToken.None);

        using var scope2 = factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db, id)!;
        job.Status.Should().Be("Succeeded");
        job.Progress.Should().Be(100);
        job.Metrics.EndedAt.Should().NotBeNull();
        File.Exists(PathHelpers.OutputPath(factory.DataRootPath, id)).Should().BeTrue();
        File.ReadAllText(PathHelpers.MarkdownPath(factory.DataRootPath, id)).Should().Contain("md");
        File.Exists(PathHelpers.ErrorPath(factory.DataRootPath, id)).Should().BeFalse();
    }
}
