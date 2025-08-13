namespace DocflowAi.Net.BBoxResolver;

/// <summary>Configuration for the pointer based resolver.</summary>
public sealed class PointerOptions
{
    /// <summary>Pointer mode returned by the LLM.</summary>
    public PointerMode Mode { get; init; } = PointerMode.WordIds;

    /// <summary>Whether pointers must be on the same page and contiguous.</summary>
    public bool Strict { get; init; } = true;

    /// <summary>Maximum allowed gap between two consecutive word IDs.</summary>
    public int MaxGapBetweenIds { get; init; } = 1;

    /// <summary>Include index map in prompt.</summary>
    public bool IncludeIndexMapInPrompt { get; init; } = true;

    /// <summary>Max tokens of pointers in prompt.</summary>
    public int MaxPointerTokensInPrompt { get; init; } = 20_000;

    /// <summary>Format of generated word IDs.</summary>
    public string WordIdFormat { get; init; } = "W{Page}_{Index}";

    /// <summary>Base confidence when strict validation passes.</summary>
    public double ConfidenceWhenStrict { get; init; } = 1.0;
}

