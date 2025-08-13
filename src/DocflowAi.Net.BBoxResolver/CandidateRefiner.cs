using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Refines candidate spans using edit distance.</summary>
internal sealed class CandidateRefiner
{
    private readonly BBoxOptions _options;

    public CandidateRefiner(BBoxOptions options) => _options = options;

    public SpanEvidence? Refine(string value, IEnumerable<WordSpan> candidates)
    {
        var normValue = DocumentIndex.Normalize(value);
        var best = default(SpanEvidence);
        var bestScore = double.NegativeInfinity;

        foreach (var c in candidates)
        {
            var candidateNorm = DocumentIndex.Normalize(c.Text);
            int dist = _options.DistanceAlgorithm == DistanceAlgorithm.ClassicLevenshtein
                ? Distance.ClassicLevenshtein(normValue, candidateNorm)
                : Distance.BitParallelMyers(normValue, candidateNorm);
            var len = Math.Max(normValue.Length, candidateNorm.Length);
            var similarity = len == 0 ? 1 : 1.0 - (double)dist / len;
            if (1 - similarity > _options.EditDistanceThreshold)
                continue;
            if (similarity > bestScore)
            {
                var indices = Enumerable.Range(c.StartWordIndexInclusive, c.EndWordIndexInclusive - c.StartWordIndexInclusive + 1).ToArray();
                best = new SpanEvidence(c.PageIndex, indices, c.BBoxUnion, c.Text, similarity, null);
                bestScore = similarity;
            }
        }
        return bestScore > double.NegativeInfinity ? best : null;
    }
}
