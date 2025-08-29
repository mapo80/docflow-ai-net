using System;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.MarkdownSystem.Repositories;
using DocflowAi.Net.Api.MarkdownSystem.Services;
using DocflowAi.Net.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class MarkdownSystemServiceTests
{
    private static JobDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new JobDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void Create_Adds_System()
    {
        using var db = CreateDb();
        var protector = Substitute.For<ISecretProtector>();
        var repo = new MarkdownSystemRepository(db, protector);
        var svc = new MarkdownSystemService(repo);
        var dto = svc.Create(new CreateMarkdownSystemRequest { Name = "s1", Provider = "docling", Endpoint = "http://x" });
        dto.Name.Should().Be("s1");
        repo.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public void Create_Duplicate_Throws()
    {
        using var db = CreateDb();
        var protector = Substitute.For<ISecretProtector>();
        var repo = new MarkdownSystemRepository(db, protector);
        var svc = new MarkdownSystemService(repo);
        svc.Create(new CreateMarkdownSystemRequest { Name = "s1", Provider = "docling", Endpoint = "http://x" });
        Action act = () => svc.Create(new CreateMarkdownSystemRequest { Name = "s1", Provider = "docling", Endpoint = "http://y" });
        act.Should().Throw<InvalidOperationException>();
    }
}
