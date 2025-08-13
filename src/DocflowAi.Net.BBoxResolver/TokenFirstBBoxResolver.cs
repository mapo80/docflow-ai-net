using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Token-first resolver that anchors fields by exact token runs with fuzzy fallback.</summary>
public sealed class TokenFirstBBoxResolver : IBBoxResolver
{
    private readonly BBoxOptions _options;

    public TokenFirstBBoxResolver(Microsoft.Extensions.Options.IOptions<BBoxOptions> options) => _options = options.Value;

    public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        var results = new ConcurrentBag<BBoxResolveResult>();
        var legacyFinder = new CandidateFinder(index);
        var legacyRefiner = new CandidateRefiner(index, _options);
        Parallel.ForEach(fields, new ParallelOptions { CancellationToken = ct }, field =>
        {
            SpanEvidence? best = ExactMatch(index, field.Value);
            if (best is null)
            {
                var candidates = legacyFinder.Find(field.Value ?? string.Empty, _options.MaxCandidates);
                best = legacyRefiner.Refine(field.Key, field.Value ?? string.Empty, candidates);
            }
            var confidence = field.Confidence;
            IReadOnlyList<SpanEvidence> spans = Array.Empty<SpanEvidence>();
            if (best is not null)
            {
                spans = new[] { best };
                confidence = 0.6 * best.Score + 0.4 * field.Confidence;
            }
            results.Add(new BBoxResolveResult(field.Key, field.Value, confidence, spans));
        });
        return Task.FromResult((IReadOnlyList<BBoxResolveResult>)results.OrderBy(r => r.FieldName).ToList());
    }

    private SpanEvidence? ExactMatch(DocumentIndex index, string? value)
    {
        var tokens = Normalizer.Tokenize(value ?? string.Empty);
        if (tokens.Length == 0)
            return null;
        foreach (var page in index.Pages)
        {
            var pageTokens = page.Words.Select(w => Normalizer.Tokenize(w.Text).FirstOrDefault() ?? string.Empty).ToArray();
            for (int i = 0; i <= pageTokens.Length - tokens.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (pageTokens[i + j] != tokens[j]) { ok = false; break; }
                }
                if (ok)
                {
                    var start = i;
                    var end = i + tokens.Length - 1;
                    var words = page.Words;
                    float minX = 1, minY = 1, maxX = 0, maxY = 0;
                    for (int k = start; k <= end; k++)
                    {
                        var b = words[k].BBox;
                        minX = Math.Min(minX, b.X);
                        minY = Math.Min(minY, b.Y);
                        maxX = Math.Max(maxX, b.X + b.W);
                        maxY = Math.Max(maxY, b.Y + b.H);
                    }
                    var indices = Enumerable.Range(start, tokens.Length).ToArray();
                    var text = string.Join(" ", words.Skip(start).Take(tokens.Length).Select(w => w.Text));
                    return new SpanEvidence(page.PageIndex, indices, new Box(minX, minY, maxX - minX, maxY - minY), text, 1.0, null);
                }
            }
        }
        return null;
    }
}
