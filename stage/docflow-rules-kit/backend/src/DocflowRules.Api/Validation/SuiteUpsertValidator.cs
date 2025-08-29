using FluentValidation;

namespace DocflowRules.Api.Validation;

public class SuiteUpsert
{
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class SuiteUpsertValidator : AbstractValidator<SuiteUpsert>
{
    public SuiteUpsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Il nome suite è obbligatorio").MaximumLength(100);
        RuleFor(x => x.Color).Matches("^#?[0-9a-fA-F]{3,8}$").When(x=>!string.IsNullOrWhiteSpace(x.Color)).WithMessage("Colore non valido (usa es. #1677ff)");
    }
}
