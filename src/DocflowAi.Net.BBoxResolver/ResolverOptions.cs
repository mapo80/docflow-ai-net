namespace DocflowAi.Net.BBoxResolver;

/// <summary>Options for selecting and configuring the bounding box resolver.</summary>
public sealed class ResolverOptions
{
    /// <summary>Resolver strategy to use.</summary>
    public ResolverStrategy Strategy { get; init; } = ResolverStrategy.Auto;

    /// <summary>Order to attempt strategies when Strategy is Auto.</summary>
    public IList<ResolverStrategy> Order { get; init; } = new List<ResolverStrategy> { ResolverStrategy.Pointer, ResolverStrategy.TokenFirst, ResolverStrategy.Legacy };

    /// <summary>Options for the TokenFirst strategy.</summary>
    public BBoxOptions TokenFirst { get; init; } = new();

    /// <summary>Options for the Pointer strategy.</summary>
    public PointerOptions Pointer { get; init; } = new();
}
