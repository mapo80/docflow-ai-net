using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Repositories;
using DocflowAi.Net.Api.Rules.Runtime;
using DocflowAi.Net.Api.Rules.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Nodes;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","RuleService")]
public class RuleServiceTests
{
    private static RuleService CreateService(out JobDbContext db, out SqliteConnection conn)
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
        return new RuleService(rules, tests, engine, runner, NullLogger<RuleService>.Instance);
    }

    [Fact]
    public async Task Create_list_update_stage_publish()
    {
        var svc = CreateService(out var db, out var conn);
        var (rule, conflict) = await svc.CreateAsync("a", null, "//code", null, null, true, default);
        conflict.Should().BeFalse();
        var (total, list) = await svc.GetAllAsync(null, null, null, 1, 20, default);
        total.Should().Be(1);
        list[0].Name.Should().Be("a");
        var res = await svc.UpdateAsync(rule!.Id, "b", null, "//code", null, null, true, default);
        res.Should().Be(RuleService.UpdateResult.Ok);
        (await svc.StageAsync(rule.Id, default)).Should().BeTrue();
        (await svc.PublishAsync(rule.Id, default)).Should().BeTrue();
        db.RuleFunctions.Single().Status.Should().Be(RuleStatus.Published);
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Duplicate_name_conflict()
    {
        var svc = CreateService(out var db, out var conn);
        await svc.CreateAsync("dup", null, "//code", null, null, true, default);
        var (_, conflict) = await svc.CreateAsync("dup", null, "//code", null, null, true, default);
        conflict.Should().BeTrue();
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Update_builtin_and_missing()
    {
        var svc = CreateService(out var db, out var conn);
        var (rule, _) = await svc.CreateAsync("x", null, "//code", null, null, true, default);
        rule!.IsBuiltin = true;
        db.SaveChanges();
        var res = await svc.UpdateAsync(rule.Id, "x1", null, "//code", null, null, true, default);
        res.Should().Be(RuleService.UpdateResult.Builtin);
        var missing = await svc.UpdateAsync(Guid.NewGuid(), "z", null, "//code", null, null, true, default);
        missing.Should().Be(RuleService.UpdateResult.NotFound);
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Compile_and_run()
    {
        var svc = CreateService(out var db, out var conn);
        var (rule, _) = await svc.CreateAsync("r", null, "set(\"x\", 1);", null, null, true, default);
        var comp = await svc.CompileAsync(rule!.Id, default);
        comp!.Value.ok.Should().BeTrue();
        var run = await svc.RunAsync(rule.Id, new JsonObject(), default);
        run!.After["x"]!.GetValue<int>().Should().Be(1);
        var (badRule, _) = await svc.CreateAsync("bad", null, "this is not code", null, null, true, default);
        var bad = await svc.CompileAsync(badRule!.Id, default);
        bad!.Value.ok.Should().BeFalse();
        bad.Value.errors.Should().NotBeEmpty();
        db.Dispose();
        conn.Dispose();
    }

    [Fact]
    public async Task Clone_with_tests()
    {
        var svc = CreateService(out var db, out var conn);
        var (rule, _) = await svc.CreateAsync("r1", null, "//code", null, null, true, default);
        db.RuleTestCases.Add(new RuleTestCase { RuleFunctionId = rule!.Id, Name = "t1", InputJson = "{}", ExpectJson = "{}", Suite = "s", TagsCsv = "a", Priority = 1, UpdatedAt = DateTimeOffset.UtcNow });
        db.SaveChanges();
        var clone = await svc.CloneAsync(rule.Id, "r2", true, default);
        clone!.Name.Should().Be("r2");
        db.RuleTestCases.Count(t => t.RuleFunctionId == clone.Id).Should().Be(1);
        db.Dispose();
        conn.Dispose();
    }
}

