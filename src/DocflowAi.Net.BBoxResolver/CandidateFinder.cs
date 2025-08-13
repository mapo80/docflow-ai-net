using Serilog;
using System.Diagnostics;
using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Finds candidate spans using token/bigram indexes with fallbacks.</summary>
internal sealed class CandidateFinder
{
    private readonly DocumentIndex _index;

    public CandidateFinder(DocumentIndex index) => _index = index;

    public IEnumerable<WordSpan> Find(string value, int maxCandidates)
    {
        var sw = Stopwatch.StartNew();
        var tokens = Normalizer.Tokenize(value);
        var valueNorm = Normalizer.Normalize(value);
        var candidates = new List<(WordSpan Span, double Score)>();

        // Stage 1: bigram runs
        if (tokens.Length >= 2)
        {
            var bigramDicts = new List<Dictionary<int, HashSet<int>>>();
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                var postings = _index.LookupBigram(tokens[i], tokens[i + 1]);
                var dict = new Dictionary<int, HashSet<int>>();
                foreach (var (page, start) in postings)
                {
                    if (!dict.TryGetValue(page, out var set))
                        dict[page] = set = new HashSet<int>();
                    set.Add(start);
                }
                bigramDicts.Add(dict);
            }
            if (bigramDicts.Count > 0)
            {
                foreach (var (page, starts) in bigramDicts[0])
                {
                    foreach (var start in starts)
                    {
                        bool ok = true;
                        for (int k = 1; k < bigramDicts.Count; k++)
                        {
                            if (!bigramDicts[k].TryGetValue(page, out var set) || !set.Contains(start + k))
                            { ok = false; break; }
                        }
                        if (ok)
                        {
                            var span = BuildSpan(page, start, start + tokens.Length - 1);
                            candidates.Add((span, tokens.Length));
                        }
                    }
                }
            }
        }

        // Stage 2: unigram peaks
        if (candidates.Count == 0 && tokens.Length > 0)
        {
            var pageVotes = new Dictionary<int, Dictionary<int, int>>();
            foreach (var token in tokens.Distinct())
            {
                foreach (var (page, word) in _index.LookupToken(token))
                {
                    if (!pageVotes.TryGetValue(page, out var dict))
                        pageVotes[page] = dict = new Dictionary<int, int>();
                    dict[word] = dict.TryGetValue(word, out var v) ? v + 1 : 1;
                }
            }
            foreach (var (page, dict) in pageVotes)
            {
                foreach (var kv in dict.OrderByDescending(k => k.Value).Take(maxCandidates))
                {
                    var span = BuildSpan(page, kv.Key, kv.Key + tokens.Length - 1);
                    candidates.Add((span, kv.Value));
                }
            }
        }

        // Stage 3: trigram fallback
        if (candidates.Count == 0)
        {
            var set = new HashSet<(int Page, int Word)>();
            foreach (var tri in Trigrams(valueNorm))
                foreach (var pos in _index.LookupTrigram(tri))
                    set.Add(pos);
            foreach (var g in set.GroupBy(x => x.Page))
            {
                var sorted = g.Select(x => x.Word).OrderBy(x => x).ToList();
                int i = 0;
                while (i < sorted.Count)
                {
                    int start = sorted[i];
                    int end = start;
                    while (i + 1 < sorted.Count && sorted[i + 1] == sorted[i] + 1)
                    { i++; end = sorted[i]; }
                    var span = BuildSpan(g.Key, start, start + tokens.Length - 1);
                    candidates.Add((span, end - start + 1));
                    i++;
                }
            }
        }

        var results = candidates
            .GroupBy(c => (c.Span.PageIndex, c.Span.StartWordIndexInclusive, c.Span.EndWordIndexInclusive))
            .Select(g => g.First())
            .OrderByDescending(c => c.Score)
            .Take(maxCandidates)
            .Select(c => c.Span)
            .ToList();

        Log.Debug("BBox:Find {@meta}", new { valueNorm, tokens = tokens.Length, stage = candidates.Count > 0 ? "ok" : "none", ms = sw.ElapsedMilliseconds, candidates = results.Count });
        return results;
    }

    private WordSpan BuildSpan(int page, int start, int end)
    {
        var words = _index.Pages[page].Words;
        start = Math.Max(0, start);
        end = Math.Min(end, words.Count - 1);
        var slice = words.Skip(start).Take(end - start + 1).ToList();
        var text = string.Join(' ', slice.Select(w => w.Text));
        var bbox = Union(slice);
        return new WordSpan(page, start, end, bbox, text);
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
