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
using System.Linq;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlamaExtractor : ILlamaExtractor, IDisposable
{
    private readonly ILogger<LlamaExtractor> _logger;
    private readonly LlmOptions _opts;
    private readonly IReasoningModeAccessor _modeAccessor;
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _ctx;
    private readonly InferenceParams _inferenceParams;

    public LlamaExtractor(
        IOptions<LlmOptions> options,
        IReasoningModeAccessor modeAccessor,
        ILogger<LlamaExtractor> logger)
    {
        _logger = logger;
        _opts = options.Value;
        _modeAccessor = modeAccessor;

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

    private string BuildSchemaText(ExtractionProfile profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You MUST output strictly valid JSON matching this schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"document_type\": string (expected: \"" + profile.DocumentType + "\"),");
        sb.AppendLine("  \"language\": string (expected: \"" + profile.Language + "\" or ISO code),");
        sb.AppendLine("  \"notes\": string,");
        sb.AppendLine("  \"fields\": [ { \"key\": string, \"value\": string, \"confidence\": number (0.0..1.0) } ]");
        sb.AppendLine("}");
        sb.AppendLine("Allowed fields and types:");
        foreach (var f in profile.Fields)
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

    public async Task<DocumentAnalysisResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fieldsSpec, CancellationToken ct)
    {
        var profile = new ExtractionProfile { Name = templateName, DocumentType = templateName, Language = "auto", Fields = fieldsSpec.ToList() };
        var schemaText = BuildSchemaText(profile);
        var rm = ResolveMode();
        var modeDirective = rm switch { ReasoningMode.Think => "/think", ReasoningMode.NoThink => "/no_think", _ => string.Empty };

        var systemPrompt = "You are an extraction engine. Extract key facts from the user-provided Markdown into STRICT JSON. Return JSON ONLY.";
        var instruction = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt + "\n";
        var userPrompt = $@"{modeDirective}
{instruction}Follow this schema and constraints:
{schemaText}

Markdown to analyze:
```markdown
{markdown}
```";

        _logger.LogInformation("Running LLM extraction template={Template} mode={Mode}", templateName, rm);

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

        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogError("LLM returned empty response");
            return new DocumentAnalysisResult(profile.DocumentType, new List<ExtractedField>(), profile.Language, null);
        }

        var start = response.IndexOf('{'); var end = response.LastIndexOf('}');
        if (start >= 0 && end > start) response = response[start..(end+1)];
        response = Regex.Replace(response, "<think>.*?</think>", "", RegexOptions.Singleline);

        JsonNode? node = null;
        try
        {
            node = JsonNode.Parse(response);
        }
        catch
        {
            try
            {
                var repaired = response.Replace("```json", "").Replace("```", "");
                node = JsonNode.Parse(repaired);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse LLM output: {Output}", response);
                return new DocumentAnalysisResult(profile.DocumentType, new List<ExtractedField>(), profile.Language, null);
            }
        }

        var docType = node?["document_type"]?.GetValue<string>() ?? profile.DocumentType;
        var lang = node?["language"]?.GetValue<string>() ?? profile.Language;
        var notes = node?["notes"]?.GetValue<string>();
        var fields = new List<ExtractedField>();
        if (node?["fields"] is JsonArray arr)
            foreach (var item in arr)
                fields.Add(new ExtractedField(item?["key"]?.GetValue<string>() ?? "", item?["value"]?.GetValue<string>(), item?["confidence"]?.GetValue<double?>() ?? 0.0));
        var result = new DocumentAnalysisResult(docType, fields, lang, notes);
        var (ok, error, fixedResult) = ExtractionValidator.ValidateAndFix(result, profile);
        if (!ok) _logger.LogWarning("Profile validation issues: {Error}", error);
        return fixedResult;
    }
    public void Dispose() { _ctx.Dispose(); _weights.Dispose(); }
}
