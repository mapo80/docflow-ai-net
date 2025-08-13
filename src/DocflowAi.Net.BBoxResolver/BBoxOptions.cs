namespace DocflowAi.Net.BBoxResolver;

/// <summary>Options controlling the bounding box resolver.</summary>
public sealed class BBoxOptions
{
    /// <summary>Distance algorithm to use.</summary>
    public DistanceAlgorithm DistanceAlgorithm { get; init; } = DistanceAlgorithm.BitParallel;
    /// <summary>Max edit distance threshold relative to max length.</summary>
    public double EditDistanceThreshold { get; init; } = 0.25;
    /// <summary>Maximum candidates to consider.</summary>
    public int MaxCandidates { get; init; } = 10;
    /// <summary>Enable label proximity heuristic.</summary>
    public bool EnableLabelProximity { get; init; } = true;
    /// <summary>Max adaptive threshold for short values (TokenFirst).</summary>
    public double AdaptiveShortMax { get; init; } = 0.40;
    /// <summary>Max adaptive threshold for long values (TokenFirst).</summary>
    public double AdaptiveLongMax { get; init; } = 0.35;
}
