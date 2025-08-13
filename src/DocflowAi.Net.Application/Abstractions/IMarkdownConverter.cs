using DocflowAi.Net.Application.Markdown;

namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Converts documents to markdown and bounding boxes.</summary>
public interface IMarkdownConverter
{
    /// <summary>Convert a PDF document to markdown.</summary>
    Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default);

    /// <summary>Convert an image to markdown.</summary>
    Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default);
}
