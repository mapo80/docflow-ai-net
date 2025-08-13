using BBoxEvalRunner.Models;
using DocflowAi.Net.BBoxResolver;

namespace BBoxEvalRunner;

public static class Evaluator
{
    public static CoverageMetrics ComputeCoverageMetrics(
        IEnumerable<string> expectedLabels,
        IEnumerable<ExtractedField> fields,
        bool pointerStrategy,
        bool strictPointer)
    {
        var expected = expectedLabels.ToList();
        var map = fields.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);

        int withBBox = 0, textOnly = 0, missing = 0, pointerValid = 0;
        var outcomes = new Dictionary<string, LabelOutcome>(StringComparer.OrdinalIgnoreCase);

        foreach (var label in expected)
        {
            if (map.TryGetValue(label, out var field))
            {
                bool hasValue = !string.IsNullOrWhiteSpace(field.Value);
                bool hasValidSpan = field.Evidence != null && field.Evidence.Any(e => e.WordIndices.Length > 0 && e.BBox.W > 0 && e.BBox.H > 0);
                if (hasValidSpan)
                {
                    withBBox++;
                    outcomes[label] = LabelOutcome.WithBBox;
                }
                else if (hasValue)
                {
                    textOnly++;
                    outcomes[label] = LabelOutcome.TextOnly;
                }
                else
                {
                    missing++;
                    outcomes[label] = LabelOutcome.Missing;
                }

                if (pointerStrategy && strictPointer)
                {
                    if (field.Pointer != null)
                    {
                        bool valid = field.Pointer.Mode switch
                        {
                            PointerMode.WordIds => field.Pointer.WordIds != null && field.Pointer.WordIds.Length > 0,
                            PointerMode.Offsets => field.Pointer.Start.HasValue && field.Pointer.End.HasValue && field.Pointer.End > field.Pointer.Start,
                            _ => false
                        };
                        if (valid) pointerValid++;
                    }
                }
            }
            else
            {
                missing++;
                outcomes[label] = LabelOutcome.Missing;
            }
        }

        int expectedCount = expected.Count;
        double coverage = expectedCount == 0 ? 0 : (double)withBBox / expectedCount;
        double extraction = expectedCount == 0 ? 0 : (double)(withBBox + textOnly) / expectedCount;
        double? pointerRate = pointerStrategy && strictPointer ? (double)pointerValid / expectedCount : null;

        return new CoverageMetrics(expectedCount, withBBox, textOnly, missing, coverage, extraction, pointerRate, outcomes);
    }

    public static Dictionary<string, Dictionary<string, LabelOutcome>> BuildHeadToHeadMatrix(
        IEnumerable<string> expectedLabels,
        IDictionary<string, IReadOnlyDictionary<string, LabelOutcome>> strategyOutcomes)
    {
        var matrix = new Dictionary<string, Dictionary<string, LabelOutcome>>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in expectedLabels)
        {
            var row = new Dictionary<string, LabelOutcome>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in strategyOutcomes)
            {
                if (kv.Value.TryGetValue(label, out var outcome))
                    row[kv.Key] = outcome;
                else
                    row[kv.Key] = LabelOutcome.Missing;
            }
            matrix[label] = row;
        }
        return matrix;
    }
}
