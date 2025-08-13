using System.Collections.Generic;
using XFundEvalRunner;
using XFundEvalRunner.Models;
using Xunit;

public class StrategyTests
{
    [Fact]
    public void Legacy_Runs_Two_Algorithms_Separately()
    {
        var cfg = new EvaluationConfig
        {
            RunLegacy = true,
            LegacyAlgos = new List<string> { "BitParallel", "ClassicLevenshtein" }
        };
        var list = StrategyEnumerator.Enumerate(cfg);
        Assert.Contains("Legacy-BitParallel", list);
        Assert.Contains("Legacy-ClassicLevenshtein", list);
    }
}
