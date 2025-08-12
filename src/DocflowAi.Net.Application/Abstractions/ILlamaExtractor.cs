using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Application.Profiles;

namespace DocflowAi.Net.Application.Abstractions;
public interface ILlamaExtractor
{
    Task<DocumentAnalysisResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fields, CancellationToken ct);
}
