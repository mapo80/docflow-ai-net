using System.Collections.Generic;
using XFundEvalRunner;
using XFundEvalRunner.Models;
using Xunit;

public class MetricsTests
{
    [Fact]
    public void Metrics_Coverage_And_IoU_AreComputed()
    {
        var expected = new[]
        {
            new FieldManifest { Name = "a", ExpectedValue = "1", ExpectedBoxes = new List<int[]> { new[] {0,0,1,1} } },
            new FieldManifest { Name = "b", ExpectedValue = "2" }
        };
        var fields = new[]
        {
            new ExtractedField("a", "1", evidence: new[] { new SpanEvidence(0, new[]{1}, new Box(0,0,1,1), "1", 1.0, null) }),
            new ExtractedField("b", "2")
        };
        var metrics = Evaluator.ComputeCoverageMetrics(expected, fields, false, false);
        Assert.Equal(2, metrics.Expected);
        Assert.Equal(1, metrics.WithBBox);
        Assert.Equal(1, metrics.TextOnly);
        Assert.Equal(0.5, metrics.CoverageRate);
        Assert.Equal(1.0, metrics.ExtractionRate);
        Assert.Equal(1.0, metrics.IoUMean);
        Assert.Equal(0.5, metrics.IoUAt0_5);
        Assert.Equal(0.5, metrics.IoUAt0_75);
    }
}
