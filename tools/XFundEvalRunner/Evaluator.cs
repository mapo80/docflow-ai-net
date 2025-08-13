using XFundEvalRunner.Models;

namespace XFundEvalRunner;

public static class Evaluator
{
    public static CoverageMetrics ComputeCoverageMetrics(
        IEnumerable<FieldManifest> expectedFields,
        IEnumerable<ExtractedField> fields,
        bool pointerStrategy,
        bool strictPointer)
    {
        var expected = expectedFields
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        var map = fields
            .GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);

        int withBBox = 0, textOnly = 0, missing = 0, pointerValid = 0;
        double iouSum = 0; int iouCount = 0; int iou50 = 0, iou75 = 0;
        var outcomes = new Dictionary<string, LabelOutcome>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in expected)
        {
            var label = kv.Key;
            var exp = kv.Value;
            if (map.TryGetValue(label, out var field))
            {
                bool hasValue = !string.IsNullOrWhiteSpace(field.Value);
                bool hasValidSpan = field.Evidence != null && field.Evidence.Any(e => e.WordIndices.Length > 0 && e.BBox.W > 0 && e.BBox.H > 0);
                if (hasValidSpan)
                {
                    withBBox++;
                    outcomes[label] = LabelOutcome.WithBBox;
                    if (exp.ExpectedBoxes.Count > 0)
                    {
                        var b = field.Evidence!.First().BBox;
                        var eb = exp.ExpectedBoxes[0];
                        var expBox = new Box(eb[0], eb[1], eb[2], eb[3]);
                        double iou = IoU(expBox, b);
                        iouSum += iou;
                        iouCount++;
                        if (iou >= 0.5) iou50++;
                        if (iou >= 0.75) iou75++;
                    }
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
        double iouMean = iouCount == 0 ? 0 : iouSum / iouCount;
        double iouAt50 = expectedCount == 0 ? 0 : (double)iou50 / expectedCount;
        double iouAt75 = expectedCount == 0 ? 0 : (double)iou75 / expectedCount;

        return new CoverageMetrics(expectedCount, withBBox, textOnly, missing, coverage, extraction, pointerRate, iouMean, iouAt50, iouAt75, outcomes);
    }

    private static double IoU(Box a, Box b)
    {
        float x1 = Math.Max(a.X, b.X);
        float y1 = Math.Max(a.Y, b.Y);
        float x2 = Math.Min(a.X + a.W, b.X + b.W);
        float y2 = Math.Min(a.Y + a.H, b.Y + b.H);
        float inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        float union = a.W * a.H + b.W * b.H - inter;
        if (union <= 0) return 0;
        return inter / union;
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
