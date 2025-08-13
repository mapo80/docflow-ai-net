namespace DocflowAi.Net.BBoxResolver;

/// <summary>Page with its dimensions and words.</summary>
/// <param name="PageIndex">Zero-based page index.</param>
/// <param name="Width">Page width.</param>
/// <param name="Height">Page height.</param>
/// <param name="Words">Words contained in the page.</param>
public sealed record Page(int PageIndex, float Width, float Height, IReadOnlyList<Word> Words);
