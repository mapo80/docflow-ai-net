using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Finds candidate spans using trigram inverted index.</summary>
internal sealed class CandidateFinder
{
    private readonly DocumentIndex _index;

    public CandidateFinder(DocumentIndex index) => _index = index;

    public IEnumerable<WordSpan> Find(string value, int maxCandidates)
    {
        var norm = DocumentIndex.Normalize(value);
        var tokens = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var triSet = Trigrams(norm).ToArray();
        if (triSet.Length == 0) yield break;

        var set = new HashSet<(int Page, int Word)>();
        foreach (var tri in triSet)
            foreach (var pos in _index.Lookup(tri))
                set.Add(pos);

        var results = new List<WordSpan>();
        foreach (var (page, wordIdx) in set)
        {
            var pageObj = _index.Pages[page];
            var len = tokens.Length;
            var start = Math.Max(0, wordIdx - 1);
            var end = Math.Min(pageObj.Words.Count - 1, start + len + 1);
            var words = pageObj.Words.Skip(start).Take(end - start + 1).ToList();
            var text = string.Join(' ', words.Select(w => w.Text));
            var bbox = Union(words);
            results.Add(new WordSpan(page, start, start + words.Count - 1, bbox, text));
        }
        foreach (var span in results.OrderByDescending(r => r.Text.Length).Take(maxCandidates))
            yield return span;
    }

    private static Box Union(IReadOnlyList<Word> words)
    {
        var minX = words.Min(w => w.BBox.X);
        var minY = words.Min(w => w.BBox.Y);
        var maxX = words.Max(w => w.BBox.X + w.BBox.W);
        var maxY = words.Max(w => w.BBox.Y + w.BBox.H);
        return new Box(minX, minY, maxX - minX, maxY - minY);
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
