using DocflowRules.Api.Validation;
using FluentAssertions;
using System.Text.Json.Nodes;
using Xunit;

namespace DocflowRules.Tests;

public class TestUpsertValidatorTests
{
    static TestUpsertPayload Make(Action<JsonObject>? cfg=null)
    {
        var expect = new JsonObject { ["fields"] = new JsonObject() };
        cfg?.Invoke((JsonObject)expect["fields"]!);
        return new TestUpsertPayload { Name = "T", Input = new JsonObject(), Expect = expect };
    }

    [Fact]
    public void EmptyFields_ShouldError()
    {
        var v = new TestUpsertValidator();
        var res = v.Validate(Make());
        res.IsValid.Should().BeFalse();
        res.Errors.Any(e => e.PropertyName.Contains("expect.fields")).Should().BeTrue();
    }

    [Fact]
    public void Exists_MustBeBoolean()
    {
        var p = Make(f => f["amount"] = new JsonObject { ["exists"] = "yes" });
        var v = new TestUpsertValidator();
        var res = v.Validate(p);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().ContainSingle(e => e.PropertyName.EndsWith(".exists"));
    }

    [Fact]
    public void Regex_MustBeStringAndValid()
    {
        var p = Make(f => f["code"] = new JsonObject { ["regex"] = 123 });
        var v = new TestUpsertValidator();
        v.Validate(p).IsValid.Should().BeFalse();

        var p2 = Make(f => f["code"] = new JsonObject { ["regex"] = "(" });
        v.Validate(p2).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Approx_NumberOrObject_AndTolNumeric()
    {
        var p1 = Make(f => f["n"] = new JsonObject { ["approx"] = "12" });
        new TestUpsertValidator().Validate(p1).IsValid.Should().BeFalse();

        var p2 = Make(f => f["n"] = new JsonObject { ["approx"] = new JsonObject { ["value"] = 10, ["tol"] = "x" } });
        new TestUpsertValidator().Validate(p2).IsValid.Should().BeFalse();

        var p3 = Make(f => f["n"] = new JsonObject { ["approx"] = 10, ["tol"] = 0.5 });
        new TestUpsertValidator().Validate(p3).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UnknownKeys_ShouldError()
    {
        var p = Make(f => f["x"] = new JsonObject { ["something"] = 1 });
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeFalse();
        res.Errors.Any(e => e.ErrorMessage.Contains("Chiave sconosciuta")).Should().BeTrue();
    }

    [Fact]
    public void AtLeastOneRule_Required()
    {
        var p = Make(f => f["x"] = new JsonObject { });
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeFalse();
        res.Errors.Any(e => e.ErrorMessage.Contains("Nessuna regola")).Should().BeTrue();
    }

    [Theory]
    [InlineData("equals")]
    [InlineData("regex")]
    [InlineData("exists")]
    public void MinimalValid_Cases(string rule)
    {
        var fields = new JsonObject();
        fields["f"] = rule switch {
            "equals" => new JsonObject { ["equals"] = 42 },
            "regex" => new JsonObject { ["regex"] = "^[A-Z]+$" },
            "exists" => new JsonObject { ["exists"] = true },
            _ => new JsonObject(),
        };
        var p = new TestUpsertPayload { Name="ok", Input=new JsonObject(), Expect=new JsonObject{{"fields", fields}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void ManyFields_MixedRules_ShouldValidate()
    {
        var fields = new JsonObject
        {
            ["a"] = new JsonObject { ["equals"] = "c" },
            ["b"] = new JsonObject { ["approx"] = 10.5, ["tol"] = 0.1 },
            ["c"] = new JsonObject { ["regex"] = "^[0-9]{3}$" },
            ["d"] = new JsonObject { ["exists"] = true }
        };
        var p = new TestUpsertPayload { Name="ok", Input=new JsonObject(), Expect=new JsonObject{{"fields", fields}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }
}
