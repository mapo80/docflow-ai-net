using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Infrastructure.Markdown.DoclingServe;
using System.Net.Http;

namespace DocflowAi.Net.Infrastructure.Markdown;

/// <summary>Markdown converter backed by Docling Serve.</summary>
public sealed class MarkdownNetConverter : IMarkdownConverter
{
    private readonly DoclingServeClient _client;

    public MarkdownNetConverter(DoclingServeClient client)
        => _client = client;

    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default)
        => ConvertAsync(pdf, ct);

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default)
        => ConvertAsync(image, ct);

    private async Task<MarkdownResult> ConvertAsync(Stream file, CancellationToken ct)
    {
        try
        {
            var res = await _client.ConvertFileAsync(file, ct);
            var md = res.Document?.MdContent ?? string.Empty;
            return new MarkdownResult(md, Array.Empty<PageInfo>(), Array.Empty<Box>());
        }
        catch (HttpRequestException ex)
        {
            throw new MarkdownConversionException("conversion_failed", ex.Message, ex);
        }
    }
}

/// <summary>Error during markdown conversion.</summary>
public sealed class MarkdownConversionException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    public MarkdownConversionException(string code, string message, Exception? inner = null, string? details = null)
        : base(message, inner)
    {
        Code = code;
        Details = details;
    }
}
