using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Orchestrates resolution using Pointer, TokenFirst and Legacy strategies.</summary>
public sealed class ResolverOrchestrator : IResolverOrchestrator
{
    private readonly ResolverOptions _options;
    private readonly IPointerResolver _pointer;
    private readonly TokenFirstBBoxResolver _tokenFirst;
    private readonly LegacyBBoxResolver _legacy;
    private readonly ILogger<ResolverOrchestrator> _logger;

    public ResolverOrchestrator(
        Microsoft.Extensions.Options.IOptions<ResolverOptions> options,
        IPointerResolver pointer,
        TokenFirstBBoxResolver tokenFirst,
        LegacyBBoxResolver legacy,
        ILogger<ResolverOrchestrator> logger)
    {
        _options = options.Value;
        _pointer = pointer;
        _tokenFirst = tokenFirst;
        _legacy = legacy;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting BBox resolution for {FieldCount} fields", fields.Count);
        var results = new BBoxResolveResult[fields.Count];
        var remaining = Enumerable.Range(0, fields.Count).ToList();

        List<ResolverStrategy> order;
        if (_options.Strategy != ResolverStrategy.Auto)
            order = new List<ResolverStrategy> { _options.Strategy };
        else
            order = _options.Order?.ToList() ?? new List<ResolverStrategy> { ResolverStrategy.Pointer, ResolverStrategy.TokenFirst, ResolverStrategy.Legacy };

        _logger.LogDebug("Resolution strategy order: {Order}", string.Join(",", order));

        foreach (var strategy in order)
        {
            if (remaining.Count == 0) break;
            _logger.LogInformation("Executing strategy {Strategy} for {Remaining} remaining fields", strategy, remaining.Count);
            switch (strategy)
            {
                case ResolverStrategy.Pointer:
                    var resolvedIdx = new List<int>();
                    foreach (var i in remaining)
                    {
                        if (_pointer.TryResolve(index, fields[i], out var res))
                        {
                            results[i] = res;
                            resolvedIdx.Add(i);
                        }
                    }
                    _logger.LogInformation("Pointer strategy resolved {Count} fields", resolvedIdx.Count);
                    foreach (var i in resolvedIdx)
                        remaining.Remove(i);
                    break;
                case ResolverStrategy.TokenFirst:
                    var tfFields = remaining.Select(i => fields[i]).ToList();
                    var tfResults = await _tokenFirst.ResolveAsync(index, tfFields, ct);
                    var toRemove = new List<int>();
                    for (int j = 0; j < tfFields.Count; j++)
                    {
                        var r = tfResults[j];
                        if (r.Spans.Count > 0)
                        {
                            results[remaining[j]] = r;
                            toRemove.Add(remaining[j]);
                        }
                    }
                    _logger.LogInformation("TokenFirst strategy resolved {Count} fields", toRemove.Count);
                    foreach (var i in toRemove)
                        remaining.Remove(i);
                    break;
                case ResolverStrategy.Legacy:
                    var lgFields = remaining.Select(i => fields[i]).ToList();
                    var lgResults = await _legacy.ResolveAsync(index, lgFields, ct);
                    for (int j = 0; j < lgFields.Count; j++)
                        results[remaining[j]] = lgResults[j];
                    remaining.Clear();
                    _logger.LogInformation("Legacy strategy resolved remaining fields");
                    break;
            }
        }

        foreach (var i in remaining)
        {
            var f = fields[i];
            results[i] = new BBoxResolveResult(f.Key, f.Value, f.Confidence, Array.Empty<SpanEvidence>());
        }
        _logger.LogInformation("BBox resolution completed");
        return results;
    }
}

