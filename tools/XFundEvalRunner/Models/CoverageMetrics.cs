namespace XFundEvalRunner.Models;

public sealed record CoverageMetrics(
    int Expected,
    int WithBBox,
    int TextOnly,
    int Missing,
    double CoverageRate,
    double ExtractionRate,
    double? PointerValidityRate,
    double IoUMean,
    double IoUAt0_5,
    double IoUAt0_75,
    IReadOnlyDictionary<string, LabelOutcome> PerLabelOutcome);
