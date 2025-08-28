namespace DocflowAi.Net.Application.Markdown;

using System.Text.Json.Serialization;

/// <summary>Options for markdown conversion.</summary>
public sealed class MarkdownOptions
{
    /// <summary>Language for OCR, e.g. "eng", "ita", or "lat".</summary>
    public string OcrLanguage { get; set; } = "ita";

    /// <summary>DPI used when rasterizing PDFs for OCR fallback.</summary>
    public int PdfRasterDpi { get; set; } = 300;

    /// <summary>Minimum number of native words required before falling back to OCR.</summary>
    public int MinimumNativeWordThreshold { get; set; } = 1;

    /// <summary>Normalize markdown output using Markdig.</summary>
    public bool NormalizeMarkdown { get; set; } = true;
}

/// <summary>Conversion result containing markdown and bounding boxes.</summary>
public sealed record MarkdownResult(
    [property: JsonPropertyOrder(2)] string Markdown,
    [property: JsonPropertyOrder(1)] IReadOnlyList<PageInfo> Pages,
    [property: JsonPropertyOrder(0)] IReadOnlyList<Box> Boxes);

/// <summary>Page information.</summary>
public sealed record PageInfo(int Number, double Width, double Height);

/// <summary>Bounding box information.</summary>
public sealed record Box(
    int Page,
    double X,
    double Y,
    double Width,
    double Height,
    double XNorm,
    double YNorm,
    double WidthNorm,
    double HeightNorm,
    string Text);
