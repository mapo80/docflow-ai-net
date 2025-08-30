using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Data;
using DocflowAi.Net.Api.Rules.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Tests;

public class RuleEntitiesTests
{
    [Fact]
    public void Seeders_add_builtin_rules_and_cases()
    {
        var builder = WebApplication.CreateBuilder();
        var dbName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<JobDbContext>(o => o.UseInMemoryDatabase(dbName));
        var app = builder.Build();

        RuleFunctionSeeder.Build(app);
        RuleTestCaseSeeder.Build(app);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.RuleFunctions.Count().Should().Be(2);
        db.RuleTestCases.Count().Should().Be(1);

        // second run should not duplicate
        RuleFunctionSeeder.Build(app);
        RuleTestCaseSeeder.Build(app);
        db.RuleFunctions.Count().Should().Be(2);
        db.RuleTestCases.Count().Should().Be(1);
    }
    [Fact]
    public void Rule_entities_support_crud_and_constraints()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        using var db = new JobDbContext(options);
        db.Database.EnsureCreated();

        var rf = new RuleFunction { Name = "r", Code = "c", CodeHash = "h" };
        var tag = new TestTag { Name = "tag" };
        var suite = new TestSuite { Name = "suite" };
        var tc = new RuleTestCase { RuleFunctionId = rf.Id, Name = "case", InputJson = "{}", ExpectJson = "{}" };
        var st = new SuggestedTest { RuleId = rf.Id, PayloadJson = "{}", Score = 0.5, Hash = "x" };
        var link = new RuleTestCaseTag { RuleTestCaseId = tc.Id, TestTagId = tag.Id };

        db.AddRange(rf, tag, suite, tc, st, link);
        db.SaveChanges();
        db.RuleTestCaseTags.Count().Should().Be(1);
        db.SuggestedTests.Single().RuleId.Should().Be(rf.Id);

        db.RuleFunctions.Add(new RuleFunction { Name = "r", Code = "c2", CodeHash = "h2" });
        db.Invoking(x => x.SaveChanges()).Should().Throw<DbUpdateException>();
        db.ChangeTracker.Clear();

        db.TestTags.Add(new TestTag { Name = "tag" });
        db.Invoking(x => x.SaveChanges()).Should().Throw<DbUpdateException>();
        db.ChangeTracker.Clear();

        db.TestSuites.Add(new TestSuite { Name = "suite" });
        db.Invoking(x => x.SaveChanges()).Should().Throw<DbUpdateException>();
    }
}
