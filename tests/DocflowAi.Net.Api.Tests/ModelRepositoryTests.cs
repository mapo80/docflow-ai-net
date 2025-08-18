using System;
using System.IO;
using System.Linq;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Api.Model.Repositories;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Application.Abstractions;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class ModelRepositoryTests
{
    private static JobDbContext CreateDb(out SqliteConnection conn)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new JobDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void AddAndUpdate_Flow_Works()
    {
        using var db = CreateDb(out var conn);
        var protector = Substitute.For<ISecretProtector>();
        protector.Protect("api").Returns("encApi");
        protector.Protect("hf").Returns("encHf");
        protector.Protect("hf2").Returns("encHf2");
        protector.Unprotect("encHf2").Returns("hf2");
        var repo = new ModelRepository(db, protector);
        var model = new ModelDocument { Id = Guid.NewGuid(), Name = "m", Type = "hosted-llm" };

        repo.Add(model, "api", "hf");
        repo.SaveChanges();
        repo.ExistsByName("m").Should().BeTrue();

        repo.SetDownloadStatus(model.Id, "downloading");
        repo.SetDownloaded(model.Id, true, "/tmp", 1, "cs");
        repo.SetDownloadLogPath(model.Id, "log");
        repo.TouchLastUsed(model.Id);

        var updated = new ModelDocument { Id = model.Id, Name = "m2", Type = "hosted-llm", Provider = "openai", BaseUrl = "u" };
        repo.Update(updated, "api2", "hf2");
        repo.SaveChanges();

        var fetched = repo.GetById(model.Id)!;
        fetched.Name.Should().Be("m2");
        repo.GetByName("m2").Should().NotBeNull();
        repo.GetHfToken(model.Id).Should().Be("hf2");

        conn.Dispose();
    }
}
