using System.Text.Json.Nodes;
using DocflowRules.Api.Validation;
using FluentAssertions;
using Xunit;

public class TestUpsertValidatorTests
{
    [Fact]
    public void Validates_expect_fields_variants()
    {
        var v = new TestUpsertValidator();
        var ok = new { Name="ok", InputJson="{ }", ExpectJson=new JsonObject { ["fields"] = new JsonObject { ["a"] = new JsonObject { ["equals"] = 1 }, ["b"] = new JsonObject { ["exists"] = true }, ["c"] = new JsonObject { ["regex"] = "^[0-9]+$" } } }.ToJsonString() };
        v.Validate(ok).IsValid.Should().BeTrue();

        var bad = new { Name="bad", InputJson="{ }", ExpectJson=new JsonObject { ["fields"] = new JsonObject { ["a"] = new JsonObject { ["unknown"] = 1 } } }.ToJsonString() };
        v.Validate(bad).IsValid.Should().BeFalse();
    }
}
