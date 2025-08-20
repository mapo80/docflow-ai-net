using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.BBoxResolver;
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
using System.Linq;
using System.IO;
using DocflowAi.Net.Application.Grammars;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlamaExtractor : ILlamaExtractor, IDisposable
{
    private readonly ILogger<LlamaExtractor> _logger;
    private readonly LlmOptions _opts;
    private readonly IReasoningModeAccessor _modeAccessor;
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _ctx;
    private readonly InferenceParams _inferenceParams;
    private readonly string? _gbnf;

    public LlamaExtractor(
        ILlmModelService modelService,
        IOptions<LlmOptions> options,
        IReasoningModeAccessor modeAccessor,
        ILogger<LlamaExtractor> logger)
    {
        _logger = logger;
        _opts = options.Value;
        _modeAccessor = modeAccessor;

        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "models");
        var info = modelService.GetCurrentModel();
        if (info.File == null)
            throw new FileNotFoundException("No model available");
        var modelPath = Path.Combine(modelsDir, info.File);
        var ctxSize = info.ContextSize ?? 4096;
        _logger.LogInformation("Loading LLama model from {ModelPath}", modelPath);
        var modelParams = new ModelParams(modelPath)
        {
            ContextSize = (uint)ctxSize,
            GpuLayerCount = 0,
            Threads = _opts.Threads
        };
        _weights = LLamaWeights.LoadFromFile(modelParams);
        _ctx = _weights.CreateContext(modelParams);

        if (_opts.UseGrammar)
        {
            var resourceName = "DocflowAi.Net.Application.Grammars.json_generic.gbnf";
            _gbnf = GrammarLoader.Load(resourceName);
        }

        var pipeline = new LLama.Sampling.DefaultSamplingPipeline
        {
            Temperature = _opts.Temperature,
            TopP = 0.9f,
            Grammar = _gbnf is null ? null : new LLama.Sampling.Grammar(_gbnf, "root")
        };

        _inferenceParams = new InferenceParams()
        {
            MaxTokens = _opts.MaxTokens,
            SamplingPipeline = pipeline
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

    public async Task<LlamaExtractionResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fieldsSpec, CancellationToken ct)
    {
        _logger.LogInformation("Preparing extraction for template={Template}", templateName);

        var profile = new ExtractionProfile { Name = templateName, DocumentType = templateName, Language = "auto", Fields = fieldsSpec.ToList() };
        var schemaText = BuildSchemaText(profile);
        _logger.LogDebug("Built schema text of length {Length}", schemaText.Length);
        var rm = ResolveMode();
        var modeDirective = rm switch { ReasoningMode.Think => "/think", ReasoningMode.NoThink => "/no_think", _ => string.Empty };

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(modeDirective))
            sb.AppendLine(modeDirective);
        sb.AppendLine("You are an extraction engine. Extract key facts from the user-provided Markdown into STRICT JSON. Return JSON ONLY.");
        if (!string.IsNullOrWhiteSpace(prompt))
            sb.AppendLine(prompt);
        sb.AppendLine("Follow this schema and constraints:");
        sb.Append(schemaText);
        var systemPrompt = sb.ToString();
        var userPrompt = markdown;

        _logger.LogInformation("Running LLM extraction template={Template} mode={Mode}", templateName, rm);

        var debugDir = Environment.GetEnvironmentVariable("DEBUG_DIR");
        if (!string.IsNullOrWhiteSpace(debugDir))
        {
            Directory.CreateDirectory(debugDir);
            File.WriteAllText(Path.Combine(debugDir, "schema_prompt.txt"), schemaText);
            var fullPrompt = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userPrompt}";
            File.WriteAllText(Path.Combine(debugDir, "final_prompt.txt"), fullPrompt);
        }

        var executor = new InteractiveExecutor(_ctx);
        var session = new ChatSession(executor);
        session.AddSystemMessage(systemPrompt);
        var raw = string.Empty;

        _logger.LogDebug("Starting chat session for template={Template}", templateName);
        await foreach (var text in session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, userPrompt),
            false,
            _inferenceParams,
            ct))
        {
            raw += text;
        }
        _logger.LogDebug("Chat session completed for template={Template}", templateName);

        _logger.LogDebug("Raw LLM output: {Output}", raw);

        DocumentAnalysisResult result;
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogError("LLM returned empty response");
            result = new DocumentAnalysisResult(profile.DocumentType, new List<ExtractedField>(), profile.Language, null);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(debugDir))
                File.WriteAllText(Path.Combine(debugDir, "llm_response.txt"), raw);
            result = ParseResult(raw, profile, templateName, _logger);
        }

        return new LlamaExtractionResult(result, systemPrompt, userPrompt);
    }
    public void Dispose() { _ctx.Dispose(); _weights.Dispose(); }

    internal static DocumentAnalysisResult ParseResult(string raw, ExtractionProfile profile, string templateName, ILogger logger)
    {
        // LLM output is constrained by a JSON grammar, but GBNF cannot enforce
        // unique property names. JsonDocument tolerates duplicates and preserves
        // their order, allowing later occurrences to override earlier ones.
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(raw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse LLM output: {Output}", raw);
            return new DocumentAnalysisResult(profile.DocumentType, new List<ExtractedField>(), profile.Language, null);
        }

        var root = doc.RootElement;
        string docType = profile.DocumentType;
        string lang = profile.Language;
        string? notes = null;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("document_type"))
                docType = prop.Value.GetString() ?? profile.DocumentType;
            else if (prop.NameEquals("language"))
                lang = prop.Value.GetString() ?? profile.Language;
            else if (prop.NameEquals("notes"))
                notes = prop.Value.GetString();
        }

        var fields = new List<ExtractedField>();
        if (root.TryGetProperty("fields", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                Pointer? ptr = null;
                if (item.TryGetProperty("wordIds", out var wids) && wids.ValueKind == JsonValueKind.Array)
                    ptr = new Pointer(PointerMode.WordIds, wids.EnumerateArray().Select(x => x.GetString()!).ToArray(), null, null);
                else if (item.TryGetProperty("offsets", out var off) && off.ValueKind == JsonValueKind.Object)
                {
                    var start = off.TryGetProperty("start", out var s) ? s.GetInt32() : (int?)null;
                    var end = off.TryGetProperty("end", out var e) ? e.GetInt32() : (int?)null;
                    ptr = new Pointer(PointerMode.Offsets, null, start, end);
                }

                var key = item.TryGetProperty("key", out var k) ? k.GetString() ?? string.Empty : string.Empty;
                var value = item.TryGetProperty("value", out var v) ? v.GetString() : null;
                var confidence = item.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.0;
                fields.Add(new ExtractedField(key, value, confidence, null, ptr));
            }
        }
        logger.LogInformation("Parsed {FieldCount} fields from LLM output", fields.Count);
        var result = new DocumentAnalysisResult(docType, fields, lang, notes);
        var (ok, error, fixedResult) = ExtractionValidator.ValidateAndFix(result, profile);
        if (!ok) logger.LogWarning("Profile validation issues: {Error}", error);
        else logger.LogInformation("Extraction successful for template={Template}", templateName);
        return fixedResult;
    }
}
