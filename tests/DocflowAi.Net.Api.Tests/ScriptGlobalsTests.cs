using DocflowAi.Net.Api.Rules.Runtime;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class ScriptGlobalsTests
{
    private static ScriptGlobals NewGlobals() => new() { Ctx = new ExtractionContext() };

    [Fact]
    public void Utilities_work()
    {
        var g = NewGlobals();
        g.Text.Trim(" a ").Should().Be("a");
        g.Text.Upper("ab").Should().Be("AB");
        g.Text.Lower("AB").Should().Be("ab");
        g.Text.Replace("aba", "a", "b").Should().Be("bbb");
        g.Money.Round(1.234m).Should().Be(1.23m);
        g.Date.Parse("2024-01-02").Should().Be(new DateOnly(2024,1,2));
        g.Date.Parse("2024/01/02 05:06").Should().Be(new DateOnly(2024,1,2));
        g.Date.Parse("bad").Should().BeNull();
        g.Iban.Normalize(" it60 x0542811101000000123456 ").Should().Be("IT60X0542811101000000123456");
        g.Iban.IsValid("IT60X0542811101000000123456").Should().BeTrue();
        g.Cf.Sex("foo").Should().Be("M");
        g.Cf.BirthDate("foo").Should().BeNull();
        g.Rx.Match("abc","a.c").Should().BeTrue();
        g.Rx.Extract("abc123","([a-z]+)(\\d+)",2).Should().Be("123");
        g.Rx.Extract("zzz","([0-9]+)").Should().BeNull();
        g.set("x",1);
        g.get<int>("x").Should().Be(1);
        g.has("x").Should().BeTrue();
        g.missing("y").Should().BeTrue();
    }

    [Fact]
    public void Assert_throws_when_false()
    {
        var g = NewGlobals();
        Action act = () => g.assert(false, "fail");
        act.Should().Throw<RuleAssertionException>().WithMessage("fail");
    }
}

