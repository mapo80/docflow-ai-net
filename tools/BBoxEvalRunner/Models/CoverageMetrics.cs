using System.Text.Json.Serialization;

namespace BBoxEvalRunner.Models;

public sealed record CoverageMetrics(
    [property: JsonPropertyName("labels_expected")] int LabelsExpected,
    [property: JsonPropertyName("labels_with_bbox")] int LabelsWithBBox,
    [property: JsonPropertyName("labels_text_only")] int LabelsTextOnly,
    [property: JsonPropertyName("labels_missing")] int LabelsMissing,
    [property: JsonPropertyName("label_coverage_rate")] double LabelCoverageRate,
    [property: JsonPropertyName("label_extraction_rate")] double LabelExtractionRate,
    [property: JsonPropertyName("label_pointer_validity_rate")] double? LabelPointerValidityRate,
    [property: JsonPropertyName("per_label_outcome")] IReadOnlyDictionary<string, LabelOutcome> PerLabelOutcome
);
