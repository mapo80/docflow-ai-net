using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Token-first resolver that anchors fields by exact token runs with fuzzy fallback.</summary>
public sealed class TokenFirstBBoxResolver : IBBoxResolver
{
    private readonly BBoxOptions _options;
    private readonly ILogger<TokenFirstBBoxResolver> _logger;

    public TokenFirstBBoxResolver(Microsoft.Extensions.Options.IOptions<BBoxOptions> options, ILogger<TokenFirstBBoxResolver> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        _logger.LogInformation("TokenFirst resolving {Count} fields", fields.Count);
        var results = new ConcurrentBag<BBoxResolveResult>();
        var legacyFinder = new CandidateFinder(index);
        var legacyRefiner = new CandidateRefiner(index, _options);
        Parallel.ForEach(fields, new ParallelOptions { CancellationToken = ct }, field =>
        {
            _logger.LogDebug("Resolving field {FieldName} via TokenFirst", field.Key);
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
                _logger.LogDebug("Field {FieldName} resolved with score {Score}", field.Key, best.Score);
            }
            else
            {
                _logger.LogDebug("Field {FieldName} could not be resolved", field.Key);
            }
            results.Add(new BBoxResolveResult(field.Key, field.Value, confidence, spans));
        });
        var ordered = results.OrderBy(r => r.FieldName).ToList();
        _logger.LogInformation("TokenFirst resolved {Count} fields", ordered.Count);
        return Task.FromResult((IReadOnlyList<BBoxResolveResult>)ordered);
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
