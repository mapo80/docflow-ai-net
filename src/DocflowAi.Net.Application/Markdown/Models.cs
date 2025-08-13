namespace DocflowAi.Net.Application.Markdown;

/// <summary>Options for markdown conversion.</summary>
public sealed record MarkdownOptions(bool NormalizeMarkdown = true);

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
