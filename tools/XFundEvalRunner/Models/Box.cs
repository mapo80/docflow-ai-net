namespace XFundEvalRunner.Models;

/// <summary>Normalized bounding box [0..1] in xywh format.</summary>
/// <param name="X">X coordinate.</param>
/// <param name="Y">Y coordinate.</param>
/// <param name="W">Width.</param>
/// <param name="H">Height.</param>
public readonly record struct Box(float X, float Y, float W, float H);
