namespace DocflowAi.Net.BBoxResolver;

/// <summary>Evidence span describing matched words.</summary>
/// <param name="Page">Page index.</param>
/// <param name="WordIndices">Indices of words within the page.</param>
/// <param name="BBox">Bounding box of the span.</param>
/// <param name="Text">Matched text.</param>
/// <param name="Score">Matching score.</param>
/// <param name="Label">Optional label evidence.</param>
public sealed record SpanEvidence(int Page, int[] WordIndices, Box BBox, string Text, double Score, LabelEvidence? Label);
