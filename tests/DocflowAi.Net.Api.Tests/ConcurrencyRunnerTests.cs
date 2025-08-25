using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using System.Linq;
using Hangfire;

namespace DocflowAi.Net.Api.Tests;

public class ConcurrencyRunnerTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ConcurrencyRunnerTests(TempDirFixture fx) => _fx = fx;

    private static JobDocument CreateQueued(Guid id, string dir, string input)
        => new()
        {
            Id = id,
            Status = "Queued",
            Progress = 0,
            Attempts = 0,
            Paths = new JobDocument.PathInfo
            {
                Dir = dir,
                Input = new JobDocument.DocumentInfo { Path = input, CreatedAt = DateTimeOffset.UtcNow },
                Prompt = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "prompt.md") },
                Output = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "output.json") },
                Error = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "error.txt") },
                Markdown = new JobDocument.DocumentInfo { Path = Path.Combine(dir, "markdown.md") }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Hash = "h",
            Metrics = new JobDocument.MetricsInfo(),
            Language = "eng",
            Engine = OcrEngine.Tesseract
        };

    [Fact]
    public async Task Respects_MaxParallelHeavyJobs()
    {
        await using var factory = new TestWebAppFactory_Step3B(_fx.RootPath, maxParallel:1);
        var store = factory.GetService<IJobRepository>();
        var uow = factory.GetService<IUnitOfWork>();
        var fs = factory.GetService<IFileSystemService>();
        var runner = factory.GetService<IJobRunner>();
        factory.Fake.CurrentMode = FakeProcessService.Mode.Slow;
        factory.Fake.SlowDelay = TimeSpan.FromSeconds(1.5);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        fs.CreateJobDirectory(id1); fs.CreateJobDirectory(id2);
        var input1 = await fs.SaveTextAtomic(id1, "input.txt", "a");
        var input2 = await fs.SaveTextAtomic(id2, "input.txt", "b");
        store.Create(CreateQueued(id1, Path.GetDirectoryName(input1)!, input1));
        store.Create(CreateQueued(id2, Path.GetDirectoryName(input2)!, input2));
        uow.SaveChanges();

        var t1 = runner.Run(id1, JobCancellationToken.Null, CancellationToken.None);
        var t2 = runner.Run(id2, JobCancellationToken.Null, CancellationToken.None);
        await Task.WhenAll(t1, t2);
        factory.Fake.MaxConcurrent.Should().Be(1);
        store.Get(id1)!.Status.Should().Be("Succeeded");
        store.Get(id2)!.Status.Should().Be("Succeeded");
    }
}
