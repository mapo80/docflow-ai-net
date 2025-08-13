namespace DocflowAi.Net.BBoxResolver;

/// <summary>Delegates resolution to the configured strategy.</summary>
public sealed class BBoxResolver : IBBoxResolver
{
    private readonly ResolverOptions _options;
    private readonly LegacyBBoxResolver _legacy;
    private readonly TokenFirstBBoxResolver _tokenFirst;

    public BBoxResolver(Microsoft.Extensions.Options.IOptions<ResolverOptions> options, LegacyBBoxResolver legacy, TokenFirstBBoxResolver tokenFirst)
    {
        _options = options.Value;
        _legacy = legacy;
        _tokenFirst = tokenFirst;
    }

    public Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default)
        => _options.Strategy == ResolverStrategy.Legacy ?
            _legacy.ResolveAsync(index, fields, ct) :
            _tokenFirst.ResolveAsync(index, fields, ct);
}
