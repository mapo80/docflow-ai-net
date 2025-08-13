namespace XFundEvalRunner.Models;

public sealed class DatasetConfig
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string ZipPath { get; set; } = "./dataset/it.val.zip";
    public string ExtractPath { get; set; } = "./dataset/xfund_it_val";
    public int MaxFiles { get; set; } = 10;
    public int RandomSeed { get; set; } = 42;
}
