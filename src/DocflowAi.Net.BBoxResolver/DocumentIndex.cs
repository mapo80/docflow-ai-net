using System.Collections.Immutable;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>
/// Immutable, thread-safe index over document words and trigram inverted lists.
/// </summary>
public sealed class DocumentIndex
{
    private readonly ImmutableArray<Page> _pages;
    private readonly IReadOnlyDictionary<string, List<(int Page, int Word)>> _trigramIndex;
    private readonly IReadOnlyDictionary<string, List<(int Page, int Word)>> _tokenIndex;
    private readonly IReadOnlyDictionary<string, List<(int Page, int Word)>> _bigramIndex;

    internal DocumentIndex(
        ImmutableArray<Page> pages,
        IReadOnlyDictionary<string, List<(int Page, int Word)>> trigramIndex,
        IReadOnlyDictionary<string, List<(int Page, int Word)>> tokenIndex,
        IReadOnlyDictionary<string, List<(int Page, int Word)>> bigramIndex)
    {
        _pages = pages;
        _trigramIndex = trigramIndex;
        _tokenIndex = tokenIndex;
        _bigramIndex = bigramIndex;
    }

    /// <summary>Pages in reading order.</summary>
    public ImmutableArray<Page> Pages => _pages;

    /// <summary>Lookup trigram postings.</summary>
    public IReadOnlyList<(int Page, int Word)> LookupTrigram(string trigram)
        => _trigramIndex.TryGetValue(trigram, out var list) ? list : Array.Empty<(int, int)>();

    /// <summary>Lookup token postings.</summary>
    public IReadOnlyList<(int Page, int Word)> LookupToken(string token)
        => _tokenIndex.TryGetValue(token, out var list) ? list : Array.Empty<(int, int)>();

    /// <summary>Lookup bigram postings.</summary>
    public IReadOnlyList<(int Page, int Word)> LookupBigram(string t1, string t2)
    {
        var key = string.Concat(t1, '\u001f', t2);
        return _bigramIndex.TryGetValue(key, out var list) ? list : Array.Empty<(int, int)>();
    }
}
