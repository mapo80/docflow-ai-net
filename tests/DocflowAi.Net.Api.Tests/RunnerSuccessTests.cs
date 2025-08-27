using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

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
        var now = DateTimeOffset.UtcNow;
        var doc = new JobDocument
        {
            Id = id,
            Status = "Queued",
            Progress = 0,
            Attempts = 0,
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = new JobDocument.DocumentInfo { Path = inputPath, CreatedAt = now },
                Prompt = new JobDocument.DocumentInfo { Path = PathHelpers.PromptPath(factory.DataRootPath, id) },
                Output = new JobDocument.DocumentInfo { Path = PathHelpers.OutputPath(factory.DataRootPath, id) },
                Error = new JobDocument.DocumentInfo { Path = PathHelpers.ErrorPath(factory.DataRootPath, id) },
                Markdown = new JobDocument.DocumentInfo { Path = PathHelpers.MarkdownPath(factory.DataRootPath, id) }
            },
            Language = "eng"
        };
        store.Create(doc);
        uow.SaveChanges();

        await runner.Run(id, JobCancellationToken.Null, CancellationToken.None);

        using var scope2 = factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<JobDbContext>();
        var job = DbTestHelper.GetJob(db, id)!;
        job.Status.Should().Be("Succeeded");
        job.Progress.Should().Be(100);
        job.Metrics.EndedAt.Should().NotBeNull();
        File.Exists(PathHelpers.OutputPath(factory.DataRootPath, id)).Should().BeTrue();
        File.ReadAllText(PathHelpers.MarkdownPath(factory.DataRootPath, id)).Should().Contain("md");
        File.Exists(PathHelpers.ErrorPath(factory.DataRootPath, id)).Should().BeFalse();
        job.Paths.Output!.CreatedAt.Should().NotBeNull();
        job.Paths.Markdown!.CreatedAt.Should().NotBeNull();
        job.Paths.Prompt!.CreatedAt.Should().NotBeNull();
    }
}
