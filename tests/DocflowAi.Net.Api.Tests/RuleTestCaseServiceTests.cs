using System.Text.Json.Nodes;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Repositories;
using DocflowAi.Net.Api.Rules.Runtime;
using DocflowAi.Net.Api.Rules.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","RuleTestCaseService")]
public class RuleTestCaseServiceTests
{
    private static RuleTestCaseService CreateService(out JobDbContext db, out SqliteConnection conn, out Guid ruleId)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        db = new JobDbContext(opts);
        db.Database.EnsureCreated();
        var rules = new RuleFunctionRepository(db);
        var tests = new RuleTestCaseRepository(db);
        var runner = new RoslynScriptRunner();
        var engine = new RuleEngine(runner);
        var svc = new RuleTestCaseService(tests, rules, engine);
        var rule = new RuleFunction { Name = "r", Code = "set(\"x\",1);", CodeHash = "h" };
        db.RuleFunctions.Add(rule);
        db.SaveChanges();
        ruleId = rule.Id;
        return svc;
    }

    [Fact]
    public async Task Crud_run_and_coverage()
    {
        var svc = CreateService(out var db, out var conn, out var ruleId);

        var expect1 = new JsonObject();
        var expect2 = new JsonObject { ["fields"] = new JsonObject { ["x"] = new JsonObject { ["value"] = 2 } } };
        var input = new JsonObject { ["fields"] = new JsonObject() };
        var t1 = await svc.CreateAsync(ruleId, "t1", input, expect1, null, null, 1, default);
        var t2 = await svc.CreateAsync(ruleId, "t2", input, expect2, null, null, 1, default);
        db.ChangeTracker.Clear();

        var (total, items) = await svc.GetAllAsync(ruleId, null, null, null, null, null, 1, 10, default);
        total.Should().Be(2);
        items.Should().HaveCount(2);

        await svc.UpdateMetaAsync(ruleId, t2.Id, "t2-upd", "suite", new[] { "a", "b" }, 2, default);
        var clone = await svc.CloneAsync(ruleId, t1.Id, "copy", null, null, default);
        clone!.Name.Should().Be("copy");

        var runAll = await svc.RunAllAsync(ruleId, default);
        runAll!.Count.Should().Be(3); // t1, t2-upd, copy
        runAll.Single(r => r.Id == t1.Id).Passed.Should().BeTrue();
        runAll.Single(r => r.Id == t2.Id).Passed.Should().BeFalse();

        var selected = await svc.RunSelectedAsync(ruleId, new[] { t1.Id }, default);
        selected!.Single().Id.Should().Be(t1.Id);

        var cov = await svc.CoverageAsync(ruleId, default);
        cov!.Single().ToString().Should().Contain("field = x");

        db.Dispose();
        conn.Dispose();
    }
}

