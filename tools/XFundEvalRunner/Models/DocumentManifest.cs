namespace XFundEvalRunner.Models;

public sealed class DocumentManifest
{
    public string File { get; set; } = string.Empty;
    public List<FieldManifest> Fields { get; set; } = new();
}
