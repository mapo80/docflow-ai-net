using System.Text.Json.Nodes;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Repositories;
using DocflowAi.Net.Api.Rules.Services;
using DocflowAi.Net.Api.Rules.Runtime;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","PropertyService")]
public class PropertyServiceTests
{
    private static PropertyTestService CreateService(string code, out Guid id)
    {
        var opts = new DbContextOptionsBuilder<JobDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new JobDbContext(opts);
        var rule = new RuleFunction { Name = "r", Code = code, CodeHash = "h" };
        db.RuleFunctions.Add(rule);
        db.SaveChanges();
        id = rule.Id;
        var rules = new RuleFunctionRepository(db);
        var tests = new RuleTestCaseRepository(db);
        var runner = new RoslynScriptRunner();
        return new PropertyTestService(rules, tests, runner);
    }

    [Fact]
    public async Task RunForRuleAsync_missing_rule_returns_empty()
    {
        var svc = CreateService("set(\"x\",1);", out var _);
        var res = await svc.RunForRuleAsync(Guid.NewGuid(), 1, 1, CancellationToken.None);
        res.Trials.Should().Be(0);
        res.Failures.Should().BeEmpty();
    }

    [Fact]
    public async Task RunForRuleAsync_detects_idempotence_failure()
    {
        var svc = CreateService("var n=get<double>(\"n\"); set(\"n\", n+1);", out var id);
        var res = await svc.RunForRuleAsync(id, 1, 1, CancellationToken.None);
        res.Failures.Single().Property.Should().Be("idempotence");
    }

    [Fact]
    public async Task RunFromBlocksAsync_runs()
    {
        var svc = CreateService("set(\"a\",1);", out var _);
        var blocks = new JsonArray { new JsonObject { ["type"]="set", ["field"]="a", ["target"]="b" } };
        var res = await svc.RunFromBlocksAsync(blocks, 1, 1, CancellationToken.None);
        res.Trials.Should().Be(1);
    }

    [Fact]
    public async Task ImportFailuresAsync_stores_tests()
    {
        var svc = CreateService("set(\"x\",1);", out var id);
        var n = await svc.ImportFailuresAsync(id, new[] { new PropertyFailure("p", new JsonObject(), "m") }, null, null, CancellationToken.None);
        n.Should().Be(1);
    }
}
