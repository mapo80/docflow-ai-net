namespace XFundEvalRunner.Models;

public sealed record CoverageMetrics(
    int Expected,
    int WithBBox,
    int TextOnly,
    int Missing,
    double CoverageRate,
    double ExtractionRate,
    double? PointerValidityRate,
    IReadOnlyDictionary<string, LabelOutcome> PerLabelOutcome);
