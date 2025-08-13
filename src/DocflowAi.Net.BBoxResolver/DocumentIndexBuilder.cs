using System.Collections.Immutable;
using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Builds a DocumentIndex from low level pages and words.</summary>
public static class DocumentIndexBuilder
{
    public sealed record SourcePage(int Page, float Width, float Height);
    public sealed record SourceWord(int Page, string Text, float XNorm, float YNorm, float WidthNorm, float HeightNorm, bool FromOcr);

    public static DocumentIndex Build(IReadOnlyList<SourcePage> pagesInput, IReadOnlyList<SourceWord> wordsInput)
    {
        var pages = ImmutableArray.CreateBuilder<Page>(pagesInput.Count);
        var trigramIndex = new Dictionary<string, List<(int Page, int Word)>>(8192);
        var tokenIndex = new Dictionary<string, List<(int Page, int Word)>>(8192);
        var bigramIndex = new Dictionary<string, List<(int Page, int Word)>>(8192);
        var grouped = wordsInput.GroupBy(w => w.Page).ToDictionary(g => g.Key, g => g.ToList());
        var pageMap = pagesInput.Select((p, idx) => (p.Page, idx)).ToDictionary(x => x.Page, x => x.idx);
        foreach (var p in pagesInput)
        {
            var idx = pageMap[p.Page];
            var words = new List<Word>();
            if (grouped.TryGetValue(p.Page, out var list))
            {
                string? prevToken = null;
                int prevIdx = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    var w = list[i];
                    var norm = Normalizer.Normalize(w.Text);
                    var word = new Word(idx, i, w.Text, norm, new Box(w.XNorm, w.YNorm, w.WidthNorm, w.HeightNorm), w.FromOcr);
                    words.Add(word);
                    foreach (var tri in Trigrams(norm))
                    {
                        if (!trigramIndex.TryGetValue(tri, out var l))
                            trigramIndex[tri] = l = new List<(int, int)>();
                        l.Add((idx, i));
                    }
                    var tokens = Normalizer.Tokenize(w.Text);
                    var token = tokens.FirstOrDefault();
                    if (token is not null)
                    {
                        if (!tokenIndex.TryGetValue(token, out var tl))
                            tokenIndex[token] = tl = new List<(int, int)>();
                        tl.Add((idx, i));
                        if (prevToken is not null)
                        {
                            var key = string.Concat(prevToken, '\u001f', token);
                            if (!bigramIndex.TryGetValue(key, out var bl))
                                bigramIndex[key] = bl = new List<(int, int)>();
                            bl.Add((idx, prevIdx));
                        }
                        prevToken = token;
                        prevIdx = i;
                    }
                    else
                    {
                        prevToken = null;
                    }
                }
            }
            pages.Add(new Page(idx, p.Width, p.Height, words));
        }
        return new DocumentIndex(pages.ToImmutable(), trigramIndex, tokenIndex, bigramIndex);
    }

    private static IEnumerable<string> Trigrams(string s)
    {
        if (s.Length < 3)
        {
            yield return s;
            yield break;
        }
        for (int i = 0; i < s.Length - 2; i++)
            yield return s.Substring(i, 3);
    }
}
