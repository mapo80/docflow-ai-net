using FluentValidation; using Microsoft.AspNetCore.Http;
namespace DocflowAi.Net.Api.Validators;
public sealed class ImageFileValidator : AbstractValidator<IFormFile> {
    private static readonly string[] Allowed = new[] { "image/png", "image/jpeg", "image/webp" };
    public ImageFileValidator() { RuleFor(f => f.Length).GreaterThan(0); RuleFor(f => f.ContentType).Must(ct => Allowed.Contains(ct ?? string.Empty)); RuleFor(f => f.FileName).NotEmpty(); }
}
