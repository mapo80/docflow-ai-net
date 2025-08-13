namespace XFundEvalRunner.Models;

public sealed class FieldManifest
{
    public string Name { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public List<int[]> ExpectedBoxes { get; set; } = new();
}
