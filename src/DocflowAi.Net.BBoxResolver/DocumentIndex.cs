using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>
/// Immutable, thread-safe index over document words and trigram inverted lists.
/// </summary>
public sealed class DocumentIndex
{
    private readonly ImmutableArray<Page> _pages;
    private readonly IReadOnlyDictionary<string, List<(int Page, int Word)>> _trigramIndex;

    internal DocumentIndex(ImmutableArray<Page> pages, IReadOnlyDictionary<string, List<(int Page, int Word)>> trigramIndex)
    {
        _pages = pages;
        _trigramIndex = trigramIndex;
    }

    /// <summary>Pages in reading order.</summary>
    public ImmutableArray<Page> Pages => _pages;

    /// <summary>Lookup trigram postings.</summary>
    public IReadOnlyList<(int Page, int Word)> Lookup(string trigram)
        => _trigramIndex.TryGetValue(trigram, out var list) ? list : Array.Empty<(int, int)>();

    /// <summary>Normalize text for indexing.</summary>
    public static string Normalize(string s)
    {
        var n = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        n = string.Join(' ', n.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        if (double.TryParse(n, NumberStyles.Any, CultureInfo.GetCultureInfo("it-IT"), out var it))
            return it.ToString(CultureInfo.InvariantCulture);
        if (double.TryParse(n, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out var en))
            return en.ToString(CultureInfo.InvariantCulture);
        return n;
    }
}
