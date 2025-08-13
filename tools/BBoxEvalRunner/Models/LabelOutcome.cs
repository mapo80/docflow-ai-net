using System.Text.Json.Serialization;

namespace BBoxEvalRunner.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LabelOutcome
{
    WithBBox,
    TextOnly,
    Missing
}
