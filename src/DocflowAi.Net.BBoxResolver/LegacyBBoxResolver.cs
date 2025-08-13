using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Legacy resolver implementation kept for backward compatibility.</summary>
public sealed class LegacyBBoxResolver : IBBoxResolver
{
    private readonly BBoxOptions _options;
    private readonly ILogger<LegacyBBoxResolver> _logger;

    public LegacyBBoxResolver(Microsoft.Extensions.Options.IOptions<BBoxOptions> options, ILogger<LegacyBBoxResolver> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        _logger.LogInformation("Legacy resolver processing {Count} fields", fields.Count);
        var results = new ConcurrentBag<BBoxResolveResult>();
        var finder = new CandidateFinder(index);
        var refiner = new CandidateRefiner(index, _options);
        Parallel.ForEach(fields, new ParallelOptions { CancellationToken = ct }, field =>
        {
            _logger.LogDebug("Resolving field {FieldName} via Legacy", field.Key);
            var candidates = finder.Find(field.Value ?? string.Empty, _options.MaxCandidates);
            var best = refiner.Refine(field.Key, field.Value ?? string.Empty, candidates);
            var confidence = field.Confidence;
            IReadOnlyList<SpanEvidence> spans = Array.Empty<SpanEvidence>();
            if (best is not null)
            {
                spans = new[] { best };
                confidence = 0.6 * best.Score + 0.4 * field.Confidence;
                _logger.LogDebug("Field {FieldName} resolved with score {Score}", field.Key, best.Score);
            }
            else
            {
                _logger.LogDebug("Field {FieldName} unresolved", field.Key);
            }
            results.Add(new BBoxResolveResult(field.Key, field.Value, confidence, spans));
        });
        var ordered = results.OrderBy(r => r.FieldName).ToList();
        _logger.LogInformation("Legacy resolver produced {Count} results", ordered.Count);
        return Task.FromResult((IReadOnlyList<BBoxResolveResult>)ordered);
    }
}
