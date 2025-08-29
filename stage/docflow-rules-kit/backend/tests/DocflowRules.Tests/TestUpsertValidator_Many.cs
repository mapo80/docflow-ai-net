using DocflowRules.Api.Validation; using FluentAssertions; using System.Text.Json.Nodes; using Xunit; namespace DocflowRules.Tests { public class TestUpsertValidator_Many { 
    [Fact]
    public void RegexValidCase_1() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f1", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_2() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f2", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_3() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f3", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_4() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f4", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_5() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f5", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_6() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f6", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_7() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f7", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_8() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f8", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_9() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f9", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_10() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f10", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_11() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f11", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_12() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f12", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_13() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f13", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_14() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f14", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_15() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f15", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_16() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f16", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_17() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f17", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_18() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f18", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_19() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f19", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_20() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f20", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_21() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f21", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_22() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f22", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_23() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f23", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_24() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f24", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_25() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f25", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_26() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f26", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_27() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f27", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_28() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f28", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_29() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f29", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void RegexValidCase_30() {
        var p = new TestUpsertPayload { Name="t", Input=new JsonObject(), Expect=new JsonObject{"fields", new JsonObject{"f30", new JsonObject{["regex"] = "^[a-z]+$"}}} };
        var res = new TestUpsertValidator().Validate(p);
        res.IsValid.Should().BeTrue(res.ToString());
    }
 } }