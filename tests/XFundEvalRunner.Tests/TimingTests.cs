using XFundEvalRunner;
using Xunit;

public class TimingTests
{
    [Fact]
    public void Timing_Aggregation_Median_P95()
    {
        var (median, p95) = TimingAggregator.Aggregate(new double[] { 10, 20, 30, 40, 50 });
        Assert.Equal(30, median);
        Assert.Equal(50, p95);
    }
}
