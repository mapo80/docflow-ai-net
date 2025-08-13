namespace DocflowAi.Net.BBoxResolver;

/// <summary>Evidence for a label located near a value.</summary>
/// <param name="Text">Label text.</param>
/// <param name="BBox">Bounding box.</param>
/// <param name="Distance">Normalized distance between label and value.</param>
public sealed record LabelEvidence(string Text, Box BBox, double Distance);
