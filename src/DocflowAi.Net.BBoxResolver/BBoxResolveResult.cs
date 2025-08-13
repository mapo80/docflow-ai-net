namespace DocflowAi.Net.BBoxResolver;

/// <summary>Result of resolving a field to bounding boxes.</summary>
/// <param name="FieldName">Name of the field.</param>
/// <param name="Value">Value of the field.</param>
/// <param name="Confidence">Final confidence score.</param>
/// <param name="Spans">Evidence spans.</param>
public sealed record BBoxResolveResult(string FieldName, string? Value, double Confidence, IReadOnlyList<SpanEvidence> Spans);
