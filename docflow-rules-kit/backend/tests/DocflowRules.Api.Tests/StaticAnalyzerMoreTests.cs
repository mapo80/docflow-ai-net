using DocflowRules.Api.Services;
using Xunit;
using FluentAssertions;

public class StaticAnalyzerMoreTests
{
    [Fact]
    public void Detects_comparators_and_equality()
    {
        var code = "if(x>=10){} if(y<=20){} if(z==5){}";
        var sa = new StaticAnalyzer();
        var (fields, tests) = sa.Analyze(code);
        tests.Should().Contain(t => t.test["suite"]!.ToString()=="boundaries");
    }
}
