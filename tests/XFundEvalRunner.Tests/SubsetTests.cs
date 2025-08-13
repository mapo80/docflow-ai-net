using XFundEvalRunner;
using XFundEvalRunner.Models;
using Xunit;

public class SubsetTests
{
    [Fact]
    public void Subset_Is_Deterministic()
    {
        var files = new[] { "b.jpg", "a.jpg", "c.jpg" };
        var cfg = new DatasetConfig { MaxFiles = 2, RandomSeed = 42 };
        var first = XFundDataset.SelectSubset(files, cfg);
        var second = XFundDataset.SelectSubset(files, cfg);
        Assert.Equal(first, second);
    }
}
