namespace XFundEvalRunner.Models;

public sealed record SpanEvidence(int Page, int[] WordIndices, Box BBox, string Text, double Score, LabelEvidence? Label);
