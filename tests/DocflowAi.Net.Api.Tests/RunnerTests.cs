using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using DocflowAi.Net.Api.Tests.Fixtures;
namespace DocflowAi.Net.Api.Tests;

public class RunnerTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public RunnerTests(TempDirFixture fx) => _fx = fx;

    private static JobDocument CreateDoc(Guid id, string dir, string input)
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
                Output = Path.Combine(dir, "output.json"),
                Error = Path.Combine(dir, "error.txt")
            }
        };

    [Fact]
    public async Task Run_Success_WritesOutput()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.AddSingleton<IProcessService>(new DelegateProcessService((_, _) =>
                    Task.FromResult(new ProcessResult(true, "{\"ok\":1}", null))))));
        var sp = factory.Services;
        var store = sp.GetRequiredService<IJobStore>();
        var fs = sp.GetRequiredService<IFileSystemService>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var inputPath = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(inputPath)!;
        store.Create(CreateDoc(id, dir, inputPath));

        await runner.Run(id, CancellationToken.None);

        var job = store.Get(id)!;
        job.Status.Should().Be("Succeeded");
        File.Exists(Path.Combine(dir, "output.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Run_Failure_WritesError()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.AddSingleton<IProcessService>(new DelegateProcessService((_, _) =>
                    Task.FromResult(new ProcessResult(false, "", "boom"))))));
        var sp = factory.Services;
        var store = sp.GetRequiredService<IJobStore>();
        var fs = sp.GetRequiredService<IFileSystemService>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input));

        await runner.Run(id, CancellationToken.None);

        var job = store.Get(id)!;
        job.Status.Should().Be("Failed");
        File.ReadAllText(Path.Combine(dir, "error.txt")).Should().Contain("boom");
    }

    [Fact]
    public async Task Run_Timeout_MarksFailed()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JobQueue:Timeouts:JobTimeoutSeconds"] = "1"
                }));
                b.ConfigureServices(s => s.AddSingleton<IProcessService>(new DelegateProcessService(async (_, ct) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    return new ProcessResult(true, "{}", null);
                })));
            });
        var sp = factory.Services;
        var store = sp.GetRequiredService<IJobStore>();
        var fs = sp.GetRequiredService<IFileSystemService>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input));

        await runner.Run(id, CancellationToken.None);
        var job = store.Get(id)!;
        job.Status.Should().Be("Failed");
        File.ReadAllText(Path.Combine(dir, "error.txt")).Should().Contain("timeout");
    }

    [Fact]
    public async Task Run_Cancelled_MarksCancelled()
    {
        var cts = new CancellationTokenSource();
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.AddSingleton<IProcessService>(new DelegateProcessService(async (_, ct) =>
                {
                    await Task.Delay(Timeout.Infinite, ct);
                    return new ProcessResult(true, "{}", null);
                }))));
        var sp = factory.Services;
        var store = sp.GetRequiredService<IJobStore>();
        var fs = sp.GetRequiredService<IFileSystemService>();
        var runner = sp.GetRequiredService<IJobRunner>();

        var id = Guid.NewGuid();
        fs.CreateJobDirectory(id);
        var input = await fs.SaveTextAtomic(id, "input.txt", "hi");
        var dir = Path.GetDirectoryName(input)!;
        store.Create(CreateDoc(id, dir, input));

        var runTask = runner.Run(id, cts.Token);
        cts.CancelAfter(100); // cancel shortly
        await runTask;

        var job = store.Get(id)!;
        job.Status.Should().Be("Cancelled");
        File.ReadAllText(Path.Combine(dir, "error.txt")).Should().Contain("cancelled");
    }
}
