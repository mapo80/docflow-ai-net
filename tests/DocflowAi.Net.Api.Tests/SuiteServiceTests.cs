using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Repositories;
using DocflowAi.Net.Api.Rules.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","SuiteService")]
public class SuiteServiceTests
{
    private static SuiteService CreateService(out JobDbContext db, out SqliteConnection conn)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        db = new JobDbContext(opts);
        db.Database.EnsureCreated();
        var repo = new TestSuiteRepository(db);
        return new SuiteService(repo, NullLogger<SuiteService>.Instance);
    }

    [Fact]
    public async Task Create_list_update_delete()
    {
        var svc = CreateService(out var db, out var conn);
        var (suite, conflict) = await svc.CreateAsync("s", "red", "d", default);
        conflict.Should().BeFalse();

        var list = await svc.GetAllAsync(default);
        list.Should().HaveCount(1);

        var ok = await svc.UpdateAsync(suite!.Id, "s2", "blue", "u", default);
        ok.Should().BeTrue();
        (await svc.GetAllAsync(default)).Single().Name.Should().Be("s2");

        await svc.DeleteAsync(suite.Id, default);
        (await svc.GetAllAsync(default)).Should().BeEmpty();

        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Prevent_duplicate_names()
    {
        var svc = CreateService(out var db, out var conn);
        await svc.CreateAsync("dup", null, null, default);
        var (_, conflict) = await svc.CreateAsync("dup", null, null, default);
        conflict.Should().BeTrue();
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Update_missing_returns_false()
    {
        var svc = CreateService(out var db, out var conn);
        var ok = await svc.UpdateAsync(Guid.NewGuid(), "x", null, null, default);
        ok.Should().BeFalse();
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Clone_flow()
    {
        var svc = CreateService(out var db, out var conn);
        var (suite, _) = await svc.CreateAsync("a", "red", null, default);
        var (clone, notFound, conflict) = await svc.CloneAsync(suite!.Id, "b", default);
        notFound.Should().BeFalse();
        conflict.Should().BeFalse();
        clone!.Name.Should().Be("b");

        var (_, nf2, _) = await svc.CloneAsync(Guid.NewGuid(), "c", default);
        nf2.Should().BeTrue();

        await svc.CreateAsync("z", null, null, default);
        var (_, nf3, cf3) = await svc.CloneAsync(suite.Id, "z", default);
        nf3.Should().BeFalse();
        cf3.Should().BeTrue();

        db.Dispose();
        conn.Dispose();
    }
}

