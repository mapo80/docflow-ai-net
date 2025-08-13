namespace XFundEvalRunner.Models;

public sealed class EvaluationConfig
{
    public bool RunPointerWordIds { get; set; }
    public bool RunPointerOffsets { get; set; }
    public bool RunTokenFirst { get; set; }
    public bool RunLegacy { get; set; }
    public List<string> LegacyAlgos { get; set; } = new();
    public int Repeat { get; set; } = 1;
    public int Warmup { get; set; } = 0;
    public bool StrictPointer { get; set; }
    public bool SavePrompts { get; set; }
    public bool SaveResponses { get; set; }
    public bool SavePerAlgorithmTrace { get; set; }
    public int MaxLogChars { get; set; } = 20000;
}
