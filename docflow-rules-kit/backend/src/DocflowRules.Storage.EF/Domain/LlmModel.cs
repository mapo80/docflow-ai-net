namespace DocflowRules.Storage.EF;

public class LlmModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "LlamaSharp"; // or OpenAI, Mock
    public string Name { get; set; } = default!;
    public string? ModelPathOrId { get; set; }    // GGUF path or OpenAI model id
    public string? Endpoint { get; set; }         // for OpenAI/Azure
    public string? ApiKey { get; set; }           // optional (vault in prod)
    public int? ContextSize { get; set; }
    public int? Threads { get; set; }
    public int? BatchSize { get; set; }
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public bool WarmupOnStart { get; set; } = false;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class LlmSettings
{
    public int Id { get; set; } = 1; // singleton
    public Guid? ActiveModelId { get; set; }
    public bool TurboProfile { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
