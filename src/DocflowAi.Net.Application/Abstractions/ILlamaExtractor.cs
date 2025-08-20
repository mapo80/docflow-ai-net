using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Application.Profiles;

namespace DocflowAi.Net.Application.Abstractions;

public record LlamaExtractionResult(DocumentAnalysisResult Analysis, string SystemPrompt, string UserPrompt);

public interface ILlamaExtractor
{
    Task<LlamaExtractionResult> ExtractAsync(
        string markdown,
        string templateName,
        string prompt,
        IReadOnlyList<FieldSpec> fields,
        CancellationToken ct,
        Func<string, string, Task>? onBeforeSend = null);
}
