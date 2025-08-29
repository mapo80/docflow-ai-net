using DocflowRules.Storage.EF;
using FluentValidation;

namespace DocflowRules.Api.Validation;

public class RuleFunctionValidator : AbstractValidator<RuleFunction>
{
    public RuleFunctionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Code).NotEmpty();
    }
}
