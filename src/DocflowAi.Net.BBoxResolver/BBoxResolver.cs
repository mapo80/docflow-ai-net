using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Default implementation of <see cref="IBBoxResolver"/>.</summary>
public sealed class BBoxResolver : IBBoxResolver
{
    private readonly BBoxOptions _options;

    public BBoxResolver(Microsoft.Extensions.Options.IOptions<BBoxOptions> options) => _options = options.Value;

    public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        var results = new ConcurrentBag<BBoxResolveResult>();
        var finder = new CandidateFinder(index);
        var refiner = new CandidateRefiner(index, _options);
        Parallel.ForEach(fields, new ParallelOptions { CancellationToken = ct }, field =>
        {
            var candidates = finder.Find(field.Value ?? string.Empty, _options.MaxCandidates);
            var best = refiner.Refine(field.Key, field.Value ?? string.Empty, candidates);
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
}
