using DocflowAi.Net.BBoxResolver;

namespace DocflowAi.Net.Domain.Extraction;
public record DocumentAnalysisResult(string DocumentType, IReadOnlyList<ExtractedField> Fields, string Language, string? Notes);
