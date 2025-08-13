namespace DocflowAi.Net.BBoxResolver;

/// <summary>
/// Normalized bounding box with origin at top-left.
/// </summary>
/// <param name="X">X coordinate [0..1].</param>
/// <param name="Y">Y coordinate [0..1].</param>
/// <param name="W">Width [0..1].</param>
/// <param name="H">Height [0..1].</param>
public record struct Box(float X, float Y, float W, float H);
