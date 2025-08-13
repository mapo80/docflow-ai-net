namespace DocflowAi.Net.BBoxResolver;

/// <summary>Field extracted by the LLM and optionally enriched with bounding boxes.</summary>
/// <param name="Key">Field name.</param>
/// <param name="Value">Field value.</param>
/// <param name="Confidence">Confidence returned by the LLM or resolver.</param>
/// <param name="Evidence">Word-level evidence spans.</param>
public sealed record ExtractedField(string Key, string? Value, double Confidence, IReadOnlyList<SpanEvidence>? Evidence = null);
