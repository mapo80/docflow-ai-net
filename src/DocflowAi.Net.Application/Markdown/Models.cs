namespace DocflowAi.Net.Application.Markdown;

/// <summary>Supported OCR engines.</summary>
public enum OcrEngine
{
    Tesseract,
    RapidOcr
}

/// <summary>Options for markdown conversion.</summary>
public sealed class MarkdownOptions
{
    /// <summary>Path to Tesseract language data (TESSDATA_PREFIX).</summary>
    public string? OcrDataPath { get; set; }

    /// <summary>Languages for OCR, e.g. "eng" or "ita+eng".</summary>
    public string OcrLanguages { get; set; } = "ita";

    /// <summary>DPI used when rasterizing PDFs for OCR fallback.</summary>
    public int PdfRasterDpi { get; set; } = 300;

    /// <summary>Minimum number of native words required before falling back to OCR.</summary>
    public int MinimumNativeWordThreshold { get; set; } = 1;

    /// <summary>Normalize markdown output using Markdig.</summary>
    public bool NormalizeMarkdown { get; set; } = true;

    /// <summary>OCR engine to use.</summary>
    public OcrEngine Engine { get; set; } = OcrEngine.Tesseract;
}

/// <summary>Conversion result containing markdown and bounding boxes.</summary>
public sealed record MarkdownResult(string Markdown, IReadOnlyList<PageInfo> Pages, IReadOnlyList<Box> Boxes);

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
