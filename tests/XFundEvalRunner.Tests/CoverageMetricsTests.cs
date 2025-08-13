using XFundEvalRunner;
using XFundEvalRunner.Models;
using Xunit;

public class CoverageMetricsTests
{
    [Fact]
    public void CoverageMetrics_Computed_From_Evidence()
    {
        var expected = new[] { "a", "b" };
        var fields = new[]
        {
            new ExtractedField("a", "1", evidence: new[] { new SpanEvidence(0, new[]{1}, new Box(0,0,1,1), "1", 1.0, null) }),
            new ExtractedField("b", "2")
        };
        var metrics = Evaluator.ComputeCoverageMetrics(expected, fields, false, false);
        Assert.Equal(2, metrics.Expected);
        Assert.Equal(1, metrics.WithBBox);
        Assert.Equal(1, metrics.TextOnly);
        Assert.Equal(0, metrics.Missing);
        Assert.Equal(0.5, metrics.CoverageRate);
        Assert.Equal(1.0, metrics.ExtractionRate);
    }
}
