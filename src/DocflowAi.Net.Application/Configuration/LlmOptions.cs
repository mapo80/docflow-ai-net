namespace DocflowAi.Net.Application.Configuration;
public sealed class LlmOptions
{
    public const string SectionName = "LLM";
    public required string ModelPath { get; set; }
    public int ContextTokens { get; set; } = 4096;
    public int Threads { get; set; } = 6;
    public int MaxTokens { get; set; } = 512;
    public float Temperature { get; set; } = 0.2f;
    public string ThinkingMode { get; set; } = "auto"; // auto|think|no_think
    public bool UseGrammar { get; set; } = true;
}
