using FluentValidation;
using System.Text.Json.Nodes;

namespace DocflowAi.Net.Api.Rules.Validation;

public class TestUpsertPayload
{
    public string Name { get; set; } = string.Empty;
    public JsonObject Input { get; set; } = new();
    public JsonObject Expect { get; set; } = new();
    public string? Suite { get; set; }
    public string[]? Tags { get; set; }
    public int? Priority { get; set; }
}

public class TestUpsertValidator : AbstractValidator<TestUpsertPayload>
{
    public TestUpsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Input).NotNull();
        RuleFor(x => x.Expect).NotNull();
        RuleFor(x => x.Expect).Must(e => e["fields"] is JsonObject)
            .WithMessage("expect.fields must be object");
    }
}
