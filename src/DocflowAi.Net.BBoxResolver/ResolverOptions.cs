namespace DocflowAi.Net.BBoxResolver;

/// <summary>Options for selecting and configuring the bounding box resolver.</summary>
public sealed class ResolverOptions
{
    /// <summary>Resolver strategy to use.</summary>
    public ResolverStrategy Strategy { get; init; } = ResolverStrategy.TokenFirst;
    /// <summary>Options for the TokenFirst strategy.</summary>
    public BBoxOptions TokenFirst { get; init; } = new();
}
