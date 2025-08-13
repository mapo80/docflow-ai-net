namespace XFundEvalRunner.Models;

public sealed class ExtractedField
{
    public string Key { get; }
    public string Value { get; }
    public double Confidence { get; }
    public Pointer? Pointer { get; }
    public IReadOnlyList<SpanEvidence>? Evidence { get; }

    public ExtractedField(string key, string value, double confidence = 1.0, Pointer? pointer = null, IReadOnlyList<SpanEvidence>? evidence = null)
    {
        Key = key;
        Value = value;
        Confidence = confidence;
        Pointer = pointer;
        Evidence = evidence;
    }
}
