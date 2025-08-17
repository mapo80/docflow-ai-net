
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class JobRepositoryFilterTests
{
    private JobDbContext Create()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        var db = new JobDbContext(opt);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void Filters_By_Status_And_Search()
    {
        using var db = Create();
        var repo = new JobRepository(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<JobRepository>());
        db.Jobs.Add(new JobDocument { Id = Guid.NewGuid(), Status = "Queued", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Paths = new JobDocument.PathInfo { Input = "invoice1.pdf" } });
        db.Jobs.Add(new JobDocument { Id = Guid.NewGuid(), Status = "Succeeded", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Paths = new JobDocument.PathInfo { Input = "report.docx" } });
        db.SaveChanges();

        var (items, total) = repo.ListPagedFiltered(1, 50, "invoice", new[] { "Queued" }, null, null, null);
        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Queued", items[0].Status);
    }
}
