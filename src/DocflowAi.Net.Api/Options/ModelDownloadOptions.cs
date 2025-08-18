namespace DocflowAi.Net.Api.Options;

public class ModelDownloadOptions
{
    public const string SectionName = "ModelDownload";
    public string LogDirectory { get; set; } = "./data/model-logs";
    public string ModelDirectory { get; set; } = "./data/models";
}
