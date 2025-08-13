using System.Text;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Builds a plain text view and word spans for offset mapping.</summary>
public sealed class PlainTextViewBuilder
{
    private readonly ILogger<PlainTextViewBuilder> _logger;

    public PlainTextViewBuilder(ILogger<PlainTextViewBuilder> logger) => _logger = logger;

    public (string textView, List<(int page, int wordIdx, int start, int len)> wordSpans) Build(DocumentIndex index)
    {
        _logger.LogDebug("Building plain text view for {PageCount} pages", index.Pages.Length);
        var sb = new StringBuilder();
        var spans = new List<(int, int, int, int)>();
        var start = 0;
        var first = true;
        foreach (var page in index.Pages)
        {
            foreach (var word in page.Words)
            {
                if (!first)
                {
                    sb.Append(' ');
                    start++;
                }
                first = false;
                var text = word.Text;
                sb.Append(text);
                spans.Add((page.PageIndex, word.WordIndex, start, text.Length));
                start += text.Length;
            }
        }
        var textView = sb.ToString();
        _logger.LogDebug("Built plain text view of length {Length}", textView.Length);
        return (textView, spans);
    }
}

