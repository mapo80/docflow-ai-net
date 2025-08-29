using System.Text.Json.Nodes;
using DocflowRules.Api.Services;
using Xunit;
using FluentAssertions;

public class StaticAnalyzerTests
{
    [Fact]
    public void Analyze_extracts_regex_numeric_and_exists()
    {
        var code = @"
            if (amount > 10) { /* ... */ }
            var r = new Regex(\"[A-Z]{3}-\\d{4}\"); 
            if (invoiceId != null) { /* ... */ }";
        var sa = new StaticAnalyzer();
        var (fields, tests) = sa.Analyze(code);
        fields.Should().Contain(new [] { "amount", "text", "invoiceId" }.As<object[]>());
        tests.Should().NotBeEmpty();
        tests.Should().Contain(t => t.test["suite"]!.ToString() == "boundaries");
        tests.Should().Contain(t => t.test["suite"]!.ToString() == "regex");
        tests.Should().Contain(t => t.test["suite"]!.ToString() == "nullability");
    }
}
