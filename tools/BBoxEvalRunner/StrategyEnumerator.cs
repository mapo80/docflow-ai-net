using BBoxEvalRunner.Models;

namespace BBoxEvalRunner;

public static class StrategyEnumerator
{
    public static IEnumerable<(string Strategy, string? DistanceAlgorithm)> Enumerate(EvaluationConfig config)
    {
        if (config.RunPointerWordIds)
            yield return ("PointerStrategy/WordIds", null);
        if (config.RunPointerOffsets)
            yield return ("PointerStrategy/Offsets", null);
        if (config.RunTokenFirst)
            yield return ("TokenFirst", null);
        if (config.RunLegacy)
        {
            foreach (var alg in config.LegacyAlgos)
                yield return ($"Legacy-{alg}", alg);
        }
    }
}
