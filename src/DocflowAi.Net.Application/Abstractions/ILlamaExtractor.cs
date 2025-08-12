using DocflowAi.Net.Domain.Extraction;
namespace DocflowAi.Net.Application.Abstractions;
public interface ILlamaExtractor { Task<DocumentAnalysisResult> ExtractAsync(string markdown, CancellationToken ct); }
