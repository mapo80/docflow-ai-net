using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Repositories;
using DocflowAi.Net.Api.Rules.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","TagService")]
public class TagServiceTests
{
    private static TagService CreateService(out JobDbContext db, out SqliteConnection conn)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        db = new JobDbContext(opts);
        db.Database.EnsureCreated();
        var repo = new TestTagRepository(db);
        return new TagService(repo, NullLogger<TagService>.Instance);
    }

    [Fact]
    public async Task Create_list_and_delete()
    {
        var svc = CreateService(out var db, out var conn);
        var (tag, conflict) = await svc.CreateAsync("t", "red", "d", default);
        conflict.Should().BeFalse();
        tag!.Name.Should().Be("t");

        var list = await svc.GetAllAsync(default);
        list.Should().HaveCount(1);

        await svc.DeleteAsync(tag.Id, default);
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
    public async Task Update_existing_and_missing()
    {
        var svc = CreateService(out var db, out var conn);
        var (tag, _) = await svc.CreateAsync("a", null, null, default);
        var ok = await svc.UpdateAsync(tag!.Id, "b", "blue", "desc", default);
        ok.Should().BeTrue();
        (await svc.GetAllAsync(default)).Single().Name.Should().Be("b");

        var missing = await svc.UpdateAsync(Guid.NewGuid(), "x", null, null, default);
        missing.Should().BeFalse();
        db.Dispose();
        conn.Dispose();
    }
}
