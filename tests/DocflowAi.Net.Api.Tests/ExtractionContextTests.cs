using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Rules.Runtime;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class ExtractionContextTests
{
    [Fact]
    public void Upsert_and_Get_work()
    {
        var ctx = new ExtractionContext();
        ctx.Has("a").Should().BeFalse();
        ctx.Upsert("a", 5, 0.8, "src");
        ctx.Has("a").Should().BeTrue();
        ctx.Get("a").Should().Be(5);
        ctx.Get<int>("a").Should().Be(5);
        ctx.Get<double>("a").Should().Be(5);
        ctx.Get<int>("missing").Should().Be(0);
        ctx.Fields.Should().ContainKey("a");
        ctx.Meta.Should().NotBeNull();
    }

    [Fact]
    public void DiffSince_detects_changes()
    {
        var ctx = new ExtractionContext();
        ctx.Upsert("a", 1);
        var before = ctx.ToJson();
        ctx.Upsert("a", 2);
        ctx.Upsert("b", "x");
        var diff = ctx.DiffSince(before);
        diff.Should().HaveCount(2);
    }

    [Fact]
    public void Get_returns_default_on_convert_failure()
    {
        var ctx = new ExtractionContext();
        ctx.Upsert("c", "abc");
        ctx.Get<int>("c").Should().Be(0);
    }
}

