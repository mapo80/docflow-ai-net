using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Rules.Runtime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Tests;

public class RoslynScriptRunnerTests
{
    [Fact]
    public async Task CompileAsync_valid_and_invalid()
    {
        var r = new RoslynScriptRunner();
        var (ok, errs) = await r.CompileAsync("set(\"a\",1);", CancellationToken.None);
        ok.Should().BeTrue();
        errs.Should().BeEmpty();
        var (ok2, errs2) = await r.CompileAsync("this is wrong", CancellationToken.None);
        ok2.Should().BeFalse();
        errs2.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_mutates_and_logs()
    {
        var r = new RoslynScriptRunner();
        var input = new JsonObject
        {
            ["fields"] = new JsonObject { ["a"] = new JsonObject { ["value"] = 1 } },
            ["meta"] = new JsonObject { ["user"] = "u" }
        };
        var code = "if(get<int>(\"a\")==1) set(\"b\",2); assert(false, \"bad\");";
        var (before, after, mut, dur, logs) = await r.RunAsync(code, input, CancellationToken.None);
        before["a"]!.GetValue<int>().Should().Be(1);
        after["b"]!.GetValue<int>().Should().Be(2);
        mut.Should().HaveCount(1);
        logs.Should().Contain(l => l.Contains("RuleAssertionException"));
    }

    [Fact]
    public void Extension_registers_services()
    {
        var services = new ServiceCollection();
        services.AddDocflowRulesCore();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IScriptRunner>().Should().BeOfType<RoslynScriptRunner>();
        provider.GetRequiredService<IRuleEngine>().Should().NotBeNull();
    }

    [Fact]
    public async Task RuleEngine_returns_result()
    {
        var services = new ServiceCollection();
        services.AddDocflowRulesCore();
        using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IRuleEngine>();
        var res = await engine.RunAsync("set(\"x\",1);", new JsonObject(), CancellationToken.None);
        res.Before.Should().BeEmpty();
        res.After["x"]!.GetValue<int>().Should().Be(1);
    }
}

