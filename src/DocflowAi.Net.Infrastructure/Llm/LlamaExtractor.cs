using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Infrastructure.Validation;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlamaExtractor : ILlamaExtractor, IDisposable
{
    private readonly ILogger<LlamaExtractor> _logger;
    private readonly LlmOptions _opts;
    private readonly ExtractionProfile _profile;
    private readonly IReasoningModeAccessor _modeAccessor;
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _ctx;
    private readonly InferenceParams _inferenceParams;

    public LlamaExtractor(
        IOptions<LlmOptions> options,
        IOptions<ExtractionProfilesOptions> profiles,
        IReasoningModeAccessor modeAccessor,
        ILogger<LlamaExtractor> logger)
    {
        _logger = logger;
        _opts = options.Value;
        _modeAccessor = modeAccessor;

        var p = profiles.Value;
        _profile = p.Profiles.FirstOrDefault(x => x.Name.Equals(p.Active, StringComparison.OrdinalIgnoreCase))
                   ?? p.Profiles.FirstOrDefault()
                   ?? new ExtractionProfile { Name = "default", DocumentType = "generic", Language = "auto" };

        _logger.LogInformation("Loading LLama model from {ModelPath}", _opts.ModelPath);
        var modelParams = new ModelParams(_opts.ModelPath)
        {
            ContextSize = (uint)_opts.ContextTokens,
            GpuLayerCount = 0,
            Threads = _opts.Threads
        };
        _weights = LLamaWeights.LoadFromFile(modelParams);
        _ctx = _weights.CreateContext(modelParams);

        _inferenceParams = new InferenceParams()
        {
            MaxTokens = _opts.MaxTokens,
            AntiPrompts = new List<string> { "</s>" }
        };
    }

    private string BuildSchemaText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You MUST output strictly valid JSON matching this schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"document_type\": string (expected: \"" + _profile.DocumentType + "\"),");
        sb.AppendLine("  \"language\": string (expected: \"" + _profile.Language + "\" or ISO code),");
        sb.AppendLine("  \"notes\": string,");
        sb.AppendLine("  \"fields\": [ { \"key\": string, \"value\": string, \"confidence\": number (0.0..1.0) } ]");
        sb.AppendLine("}");
        sb.AppendLine("Allowed fields and types:");
        foreach (var f in _profile.Fields)
            sb.AppendLine($"- {f.Key} : {f.Type} {(f.Required ? "(required)" : "")} - {f.Description}");
        sb.AppendLine("Do NOT include explanations or code fences. Output JSON only.");
        return sb.ToString();
    }

    private ReasoningMode ResolveMode()
    {
        var def = _opts.ThinkingMode?.ToLowerInvariant() switch
        {
            "think" => ReasoningMode.Think,
            "no_think" => ReasoningMode.NoThink,
            _ => ReasoningMode.Auto
        };
        return _modeAccessor.Mode != ReasoningMode.Auto ? _modeAccessor.Mode : def;
    }

    public async Task<DocumentAnalysisResult> ExtractAsync(string markdown, CancellationToken ct)
    {
        var schemaText = BuildSchemaText();
        var rm = ResolveMode();
        var modeDirective = rm switch { ReasoningMode.Think => "/think", ReasoningMode.NoThink => "/no_think", _ => string.Empty };

        var systemPrompt = "You are an extraction engine. Extract key facts from the user-provided Markdown into STRICT JSON. Return JSON ONLY.";
        var userPrompt = $@"{modeDirective}
Follow this schema and constraints:
{schemaText}

Markdown to analyze:
```markdown
{markdown}
```";

        _logger.LogInformation("Running LLM extraction profile={Profile} mode={Mode}", _profile.Name, rm);

        var executor = new InteractiveExecutor(_ctx);
        var session = new ChatSession(executor);
        session.AddSystemMessage(systemPrompt);
        var response = string.Empty;

        await foreach (var text in session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, userPrompt),
            false,
            _inferenceParams,
            ct))
        {
            response += text;
        }

        _logger.LogDebug("Raw LLM output: {Output}", response);

        var start = response.IndexOf('{'); var end = response.LastIndexOf('}');
        if (start >= 0 && end > start) response = response[start..(end+1)];
        response = Regex.Replace(response, "<think>.*?</think>", "", RegexOptions.Singleline);

        JsonNode? node;
        try { node = JsonNode.Parse(response); }
        catch { var repaired = response.Replace("```json","").Replace("```",""); node = JsonNode.Parse(repaired); }

        var docType = node?["document_type"]?.GetValue<string>() ?? _profile.DocumentType;
        var lang = node?["language"]?.GetValue<string>() ?? _profile.Language;
        var notes = node?["notes"]?.GetValue<string>();
        var fields = new List<ExtractedField>();
        if (node?["fields"] is JsonArray arr)
            foreach (var item in arr)
                fields.Add(new ExtractedField(item?["key"]?.GetValue<string>() ?? "", item?["value"]?.GetValue<string>(), item?["confidence"]?.GetValue<double?>() ?? 0.0));
        var result = new DocumentAnalysisResult(docType, fields, lang, notes);
        var (ok, error, fixedResult) = ExtractionValidator.ValidateAndFix(result, _profile);
        if (!ok) _logger.LogWarning("Profile validation issues: {Error}", error);
        return fixedResult;
    }
    public void Dispose() { _ctx.Dispose(); _weights.Dispose(); }
}
