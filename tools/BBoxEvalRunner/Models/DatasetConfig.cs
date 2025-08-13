namespace BBoxEvalRunner.Models;

public sealed class DatasetConfig
{
    public string Path { get; set; } = "./dataset";
    public List<string> Fields { get; set; } = new();
    public string? Manifest { get; set; }
}

public sealed class ManifestEntry
{
    public string File { get; set; } = string.Empty;
    public List<string> Fields { get; set; } = new();
}
