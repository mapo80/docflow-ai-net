namespace XFundEvalRunner;

public static class PointerFallbackAggregator
{
    public static IReadOnlyDictionary<string, int> Aggregate(IEnumerable<string> reasons)
    {
        return reasons
            .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }
}
