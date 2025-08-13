namespace DocflowAi.Net.BBoxResolver;

/// <summary>Orchestrates resolution using Pointer, TokenFirst and Legacy strategies.</summary>
public sealed class ResolverOrchestrator : IResolverOrchestrator
{
    private readonly ResolverOptions _options;
    private readonly IPointerResolver _pointer;
    private readonly TokenFirstBBoxResolver _tokenFirst;
    private readonly LegacyBBoxResolver _legacy;

    public ResolverOrchestrator(
        Microsoft.Extensions.Options.IOptions<ResolverOptions> options,
        IPointerResolver pointer,
        TokenFirstBBoxResolver tokenFirst,
        LegacyBBoxResolver legacy)
    {
        _options = options.Value;
        _pointer = pointer;
        _tokenFirst = tokenFirst;
        _legacy = legacy;
    }

    public async Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
    {
        var results = new BBoxResolveResult[fields.Count];
        var remaining = Enumerable.Range(0, fields.Count).ToList();

        List<ResolverStrategy> order;
        if (_options.Strategy != ResolverStrategy.Auto)
            order = new List<ResolverStrategy> { _options.Strategy };
        else
            order = _options.Order?.ToList() ?? new List<ResolverStrategy> { ResolverStrategy.Pointer, ResolverStrategy.TokenFirst, ResolverStrategy.Legacy };

        foreach (var strategy in order)
        {
            if (remaining.Count == 0) break;
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
                    foreach (var i in toRemove)
                        remaining.Remove(i);
                    break;
                case ResolverStrategy.Legacy:
                    var lgFields = remaining.Select(i => fields[i]).ToList();
                    var lgResults = await _legacy.ResolveAsync(index, lgFields, ct);
                    for (int j = 0; j < lgFields.Count; j++)
                        results[remaining[j]] = lgResults[j];
                    remaining.Clear();
                    break;
            }
        }

        foreach (var i in remaining)
        {
            var f = fields[i];
            results[i] = new BBoxResolveResult(f.Key, f.Value, f.Confidence, Array.Empty<SpanEvidence>());
        }
        return results;
    }
}

