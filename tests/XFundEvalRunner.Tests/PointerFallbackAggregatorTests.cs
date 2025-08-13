using XFundEvalRunner;
using Xunit;

public class PointerFallbackAggregatorTests
{
    [Fact]
    public void Pointer_Fallback_Reasons_Aggregated()
    {
        var reasons = new[] { "NoPointers", "InvalidIds", "NoPointers" };
        var agg = PointerFallbackAggregator.Aggregate(reasons);
        Assert.Equal(2, agg["NoPointers"]);
        Assert.Equal(1, agg["InvalidIds"]);
    }
}
