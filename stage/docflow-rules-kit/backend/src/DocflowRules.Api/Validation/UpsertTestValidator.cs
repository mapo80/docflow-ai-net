using FluentValidation;
using System.Text.Json.Nodes;

namespace DocflowRules.Api.Validation;

public class UpsertTestValidator : AbstractValidator<object>
{
    public UpsertTestValidator()
    {
        RuleFor(x => x).NotNull();
    }
}
