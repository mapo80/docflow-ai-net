using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Application.Profiles;
using Microsoft.AspNetCore.Http;

namespace DocflowAi.Net.Application.Abstractions;
public interface IProcessingOrchestrator
{
    Task<DocumentAnalysisResult> ProcessAsync(IFormFile file, string templateName, string prompt, IReadOnlyList<FieldSpec> fields, CancellationToken ct);
}
