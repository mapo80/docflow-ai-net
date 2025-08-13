namespace DocflowAi.Net.BBoxResolver;

/// <summary>Represents a contiguous span of words.</summary>
/// <param name="PageIndex">Page index containing the span.</param>
/// <param name="StartWordIndexInclusive">Start word index inclusive.</param>
/// <param name="EndWordIndexInclusive">End word index inclusive.</param>
/// <param name="BBoxUnion">Bounding box covering the span.</param>
/// <param name="Text">Concatenated text of the span.</param>
public sealed record WordSpan(int PageIndex, int StartWordIndexInclusive, int EndWordIndexInclusive, Box BBoxUnion, string Text);
