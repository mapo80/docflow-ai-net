namespace DocflowAi.Net.Application.Configuration;
public sealed class LlmOptions
{
    public const string SectionName = "LLM";
    public string? DefaultModelRepo { get; set; }
    public string? DefaultModelFile { get; set; }
    public int Threads { get; set; } = 6;
    public int MaxTokens { get; set; } = 512;
    public float Temperature { get; set; } = 0.2f;
    public string ThinkingMode { get; set; } = "auto"; // auto|think|no_think
    public bool UseGrammar { get; set; } = true;
}
