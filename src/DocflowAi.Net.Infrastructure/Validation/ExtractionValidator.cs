using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.BBoxResolver;
namespace DocflowAi.Net.Infrastructure.Validation;
public static class ExtractionValidator {
    public static (bool ok, string? error, DocumentAnalysisResult fixedResult) ValidateAndFix(DocumentAnalysisResult input, ExtractionProfile profile) {
        var fields = input.Fields.ToList(); var errors = new List<string>();
        foreach (var spec in profile.Fields.Where(f => f.Required)) {
            if (!fields.Any(f => string.Equals(f.Key, spec.Key, StringComparison.OrdinalIgnoreCase))) {
                errors.Add($"Missing required field: {spec.Key}"); fields.Add(new ExtractedField(spec.Key, null, 0.0));
            }
        }
        List<ExtractedField> fixedFields = new();
        foreach (var f in fields) {
            var spec = profile.Fields.FirstOrDefault(s => string.Equals(s.Key, f.Key, StringComparison.OrdinalIgnoreCase));
            if (spec is null) { fixedFields.Add(new ExtractedField(f.Key, f.Value, Math.Min(f.Confidence, 0.5))); continue; }
            if (spec.Type == "number" && f.Value is not null) {
                var okNum = double.TryParse(f.Value.Replace(",", ".").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);
                if (!okNum) errors.Add($"Field {f.Key} expected number, got '{f.Value}'");
            }
            fixedFields.Add(f);
        }
        var result = new DocumentAnalysisResult(
            string.IsNullOrWhiteSpace(input.DocumentType) ? profile.DocumentType : input.DocumentType,
            fixedFields, string.IsNullOrWhiteSpace(input.Language) ? profile.Language : input.Language, input.Notes);
        return (errors.Count == 0, errors.Count == 0 ? null : string.Join("; ", errors), result);
    }
}
