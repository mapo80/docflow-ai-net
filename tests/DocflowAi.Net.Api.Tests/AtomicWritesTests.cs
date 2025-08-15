using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class AtomicWritesTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public AtomicWritesTests(TempDirFixture fx) => _fx = fx;

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
                Error = PathHelpers.ErrorPath(dataRoot, id)
            }
        };

    [Fact]
    public async Task No_Tmp_Files_Left_After_Run()
    {
        await using var factory = new TestWebAppFactory_Step3A(_fx.RootPath);
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var fs = sp.GetRequiredService<IFileSystemService>();
        var store = sp.GetRequiredService<IJobRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var runner = sp.GetRequiredService<IJobRunner>();

        // success case
        factory.Fake.CurrentMode = FakeProcessService.Mode.Success;
        var id1 = Guid.NewGuid();
        fs.CreateJobDirectory(id1);
        var input1 = await fs.SaveTextAtomic(id1, "input.txt", "hi");
        var dir1 = Path.GetDirectoryName(input1)!;
        store.Create(CreateDoc(id1, dir1, input1, factory.DataRootPath));
        uow.SaveChanges();
        await runner.Run(id1, CancellationToken.None);
        Directory.EnumerateFiles(PathHelpers.JobDir(factory.DataRootPath, id1), "*.tmp", SearchOption.AllDirectories)
            .Should().BeEmpty();

        // failure case
        factory.Fake.CurrentMode = FakeProcessService.Mode.Fail;
        var id2 = Guid.NewGuid();
        fs.CreateJobDirectory(id2);
        var input2 = await fs.SaveTextAtomic(id2, "input.txt", "hi");
        var dir2 = Path.GetDirectoryName(input2)!;
        store.Create(CreateDoc(id2, dir2, input2, factory.DataRootPath));
        uow.SaveChanges();
        await runner.Run(id2, CancellationToken.None);
        Directory.EnumerateFiles(PathHelpers.JobDir(factory.DataRootPath, id2), "*.tmp", SearchOption.AllDirectories)
            .Should().BeEmpty();
    }
}
