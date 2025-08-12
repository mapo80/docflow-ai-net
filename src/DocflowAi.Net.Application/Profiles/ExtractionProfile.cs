namespace DocflowAi.Net.Application.Profiles;
public sealed class ExtractionProfile {
    public required string Name { get; set; }
    public string DocumentType { get; set; } = "generic";
    public string Language { get; set; } = "auto";
    public List<FieldSpec> Fields { get; set; } = new();
}
public sealed class FieldSpec {
    public required string Key { get; set; }
    public string Description { get; set; } = "";
    public string Type { get; set; } = "string"; // string|number|date
    public bool Required { get; set; } = false;
}
