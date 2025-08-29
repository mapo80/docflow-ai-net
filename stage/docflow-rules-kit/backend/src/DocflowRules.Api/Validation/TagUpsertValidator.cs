using FluentValidation;

namespace DocflowRules.Api.Validation;

public class TagUpsert
{
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class TagUpsertValidator : AbstractValidator<TagUpsert>
{
    public TagUpsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Il nome tag è obbligatorio").MaximumLength(100);
        RuleFor(x => x.Color).Matches("^#?[0-9a-fA-F]{3,8}$").When(x=>!string.IsNullOrWhiteSpace(x.Color)).WithMessage("Colore non valido (usa es. #52c41a)");
    }
}
