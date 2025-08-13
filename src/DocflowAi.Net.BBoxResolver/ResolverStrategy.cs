namespace DocflowAi.Net.BBoxResolver;

/// <summary>Strategy used by the bounding box resolver.</summary>
public enum ResolverStrategy
{
    /// <summary>Automatically choose the best strategy.</summary>
    Auto = 0,
    /// <summary>Use explicit pointers returned by the LLM.</summary>
    Pointer = 1,
    /// <summary>Use the TokenFirst strategy.</summary>
    TokenFirst = 2,
    /// <summary>Use the legacy resolver.</summary>
    Legacy = 3
}
