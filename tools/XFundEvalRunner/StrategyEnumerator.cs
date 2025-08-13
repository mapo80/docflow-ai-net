using XFundEvalRunner.Models;

namespace XFundEvalRunner;

public static class StrategyEnumerator
{
    public static IReadOnlyList<string> Enumerate(EvaluationConfig config)
    {
        var list = new List<string>();
        if (config.RunPointerWordIds) list.Add("PointerWordIds");
        if (config.RunPointerOffsets) list.Add("PointerOffsets");
        if (config.RunTokenFirst) list.Add("TokenFirst");
        if (config.RunLegacy)
        {
            foreach (var algo in config.LegacyAlgos)
                list.Add($"Legacy-{algo}");
        }
        return list;
    }
}
