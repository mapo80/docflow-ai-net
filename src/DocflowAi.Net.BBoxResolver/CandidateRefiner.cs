using Serilog;
using System.Diagnostics;
using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Refines candidate spans using edit distance and label proximity.</summary>
internal sealed class CandidateRefiner
{
    private readonly BBoxOptions _options;
    private readonly DocumentIndex _index;

    public CandidateRefiner(DocumentIndex index, BBoxOptions options)
    {
        _index = index;
        _options = options;
    }

    public SpanEvidence? Refine(string fieldName, string value, IEnumerable<WordSpan> candidates)
    {
        var sw = Stopwatch.StartNew();
        var list = candidates.ToList();
        var normValue = Normalizer.Normalize(value);
        var threshold = _options.EditDistanceThreshold;
        var best = Evaluate(list, normValue, threshold, fieldName);
        if (best is null)
        {
            threshold = normValue.Length <= 8 ? Math.Max(threshold, _options.AdaptiveShortMax) : Math.Max(threshold, _options.AdaptiveLongMax);
            best = Evaluate(list, normValue, threshold, fieldName);
        }
        Log.Debug("BBox:Refine {@meta}", new { Alg = _options.DistanceAlgorithm.ToString(), Threshold = threshold, Candidates = list.Count, Similarity = best?.Score, ms = sw.ElapsedMilliseconds });
        return best;
    }

    private SpanEvidence? Evaluate(List<WordSpan> candidates, string normValue, double threshold, string fieldName)
    {
        SpanEvidence? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var c in candidates)
        {
            var candidateNorm = Normalizer.Normalize(c.Text);
            int dist = _options.DistanceAlgorithm == DistanceAlgorithm.ClassicLevenshtein
                ? Distance.ClassicLevenshtein(normValue, candidateNorm)
                : Distance.BitParallelMyers(normValue, candidateNorm);
            var len = Math.Max(normValue.Length, candidateNorm.Length);
            var similarity = len == 0 ? 1 : 1.0 - (double)dist / len;
            if (1 - similarity > threshold)
                continue;
            LabelEvidence? label = null;
            if (_options.EnableLabelProximity)
                label = FindNearestLabel(fieldName, c);
            if (similarity > bestScore || (Math.Abs(similarity - bestScore) < 1e-6 && Better(label, best?.Label)))
            {
                var indices = Enumerable.Range(c.StartWordIndexInclusive, c.EndWordIndexInclusive - c.StartWordIndexInclusive + 1).ToArray();
                best = new SpanEvidence(c.PageIndex, indices, c.BBoxUnion, c.Text, similarity, label);
                bestScore = similarity;
            }
        }
        return best;
    }

    private LabelEvidence? FindNearestLabel(string fieldName, WordSpan span)
    {
        var tokens = Normalizer.Tokenize(fieldName);
        if (tokens.Length == 0)
            return null;
        LabelEvidence? best = null;
        foreach (var (page, word) in _index.LookupToken(tokens[0]))
        {
            if (page != span.PageIndex) continue;
            var w = _index.Pages[page].Words[word];
            var dist = EuclideanDistance(w.BBox, span.BBoxUnion);
            if (best is null || dist < best.Distance)
                best = new LabelEvidence(w.Text, w.BBox, dist);
        }
        return best;
    }

    private static double EuclideanDistance(Box a, Box b)
    {
        var ax = a.X + a.W / 2;
        var ay = a.Y + a.H / 2;
        var bx = b.X + b.W / 2;
        var by = b.Y + b.H / 2;
        var dx = ax - bx;
        var dy = ay - by;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool Better(LabelEvidence? a, LabelEvidence? b)
    {
        if (a is null && b is null) return false;
        if (a is not null && b is null) return true;
        if (a is null) return false;
        return a.Distance < b!.Distance;
    }
}
