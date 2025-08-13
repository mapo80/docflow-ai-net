using System.Text;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Builds word identifier maps and index map text.</summary>
public sealed class WordIdMapBuilder
{
    private readonly PointerOptions _options;

    public WordIdMapBuilder(Microsoft.Extensions.Options.IOptions<PointerOptions> options)
        => _options = options.Value;

    public (Dictionary<(int page, int wordIdx), string> map, Dictionary<string, (int page, int wordIdx)> reverse, string indexMap) Build(DocumentIndex index)
    {
        var map = new Dictionary<(int, int), string>();
        var reverse = new Dictionary<string, (int, int)>();
        var sb = new StringBuilder();
        sb.AppendLine("<<INDEX_MAP_BEGIN>>");
        foreach (var page in index.Pages)
        {
            sb.AppendLine($"Page {page.PageIndex}:");
            foreach (var word in page.Words)
            {
                var id = _options.WordIdFormat
                    .Replace("{Page}", page.PageIndex.ToString())
                    .Replace("{Index}", word.WordIndex.ToString());
                map[(page.PageIndex, word.WordIndex)] = id;
                reverse[id] = (page.PageIndex, word.WordIndex);
                sb.Append(word.Norm);
                sb.Append("[[");
                sb.Append(id);
                sb.Append("]] ");
            }
            sb.AppendLine();
        }
        sb.AppendLine("<<INDEX_MAP_END>>");
        return (map, reverse, sb.ToString());
    }
}

