namespace DocflowAi.Net.BBoxResolver;

/// <summary>Single word within a page.</summary>
/// <param name="PageIndex">Zero-based page index.</param>
/// <param name="WordIndex">Zero-based word index within the page.</param>
/// <param name="Text">Original text.</param>
/// <param name="Norm">Normalized text.</param>
/// <param name="BBox">Word bounding box.</param>
/// <param name="FromOcr">True if originated from OCR.</param>
public sealed record Word(int PageIndex, int WordIndex, string Text, string Norm, Box BBox, bool FromOcr);
