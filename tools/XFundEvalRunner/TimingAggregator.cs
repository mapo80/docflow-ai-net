namespace XFundEvalRunner;

public static class TimingAggregator
{
    public static (double Median, double P95) Aggregate(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0) return (0, 0);
        var ordered = samples.OrderBy(x => x).ToArray();
        double median = ordered[ordered.Length / 2];
        int p95Index = (int)Math.Ceiling(0.95 * ordered.Length) - 1;
        if (p95Index < 0) p95Index = 0;
        double p95 = ordered[Math.Min(p95Index, ordered.Length - 1)];
        return (median, p95);
    }
}
