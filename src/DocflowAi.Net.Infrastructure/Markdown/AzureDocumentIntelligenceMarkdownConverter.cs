using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DocflowAi.Net.Infrastructure.Markdown;

/// <summary>Markdown converter backed by Azure Document Intelligence.</summary>
public sealed class AzureDocumentIntelligenceMarkdownConverter : IMarkdownConverter
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceMarkdownConverter(Uri endpoint, string? apiKey)
    {
        _client = !string.IsNullOrEmpty(apiKey)
            ? new DocumentIntelligenceClient(endpoint, new AzureKeyCredential(apiKey))
            : new DocumentIntelligenceClient(endpoint, new DefaultAzureCredential());
    }

    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => ConvertAsync(pdf, ct);

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => ConvertAsync(image, ct);

    private async Task<MarkdownResult> ConvertAsync(Stream file, CancellationToken ct)
    {
        var options = new AnalyzeDocumentOptions("prebuilt-layout", BinaryData.FromStream(file))
        {
            OutputContentFormat = DocumentContentFormat.Markdown
        };
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, options, ct);
        string rawJson;
        var contentStream = operation.GetRawResponse().ContentStream ?? Stream.Null;
        using (var reader = new StreamReader(contentStream, Encoding.UTF8))
        {
            rawJson = await reader.ReadToEndAsync();
        }
        var result = operation.Value;

        var pages = result.Pages.Select(p => new PageInfo(p.PageNumber, p.Width ?? 0, p.Height ?? 0)).ToList();
        var boxes = new List<Box>();

        foreach (var page in result.Pages)
        {
            var width = page.Width ?? 0;
            var height = page.Height ?? 0;
            foreach (var word in page.Words)
            {
                var poly = word.Polygon;
                var xs = new[] { poly[0], poly[2], poly[4], poly[6] };
                var ys = new[] { poly[1], poly[3], poly[5], poly[7] };
                var x = xs.Min();
                var y = ys.Min();
                var w = xs.Max() - x;
                var h = ys.Max() - y;
                var xNorm = width > 0 ? x / width : 0;
                var yNorm = height > 0 ? y / height : 0;
                var wNorm = width > 0 ? w / width : 0;
                var hNorm = height > 0 ? h / height : 0;
                boxes.Add(new Box(page.PageNumber, x, y, w, h, xNorm, yNorm, wNorm, hNorm, word.Content));
            }
        }

        return new MarkdownResult(result.Content, pages, boxes, rawJson);
    }
}
