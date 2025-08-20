using System.Diagnostics;
using System.Text.Json;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.BBoxResolver;
using DocflowAi.Net.Domain.Extraction;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Executes the full document processing pipeline: markdown conversion,
/// LLM extraction and bounding box resolution.
/// </summary>
public class ProcessService : IProcessService
{
    private readonly ITemplateRepository _templates;
    private readonly IMarkdownConverter _converter;
    private readonly ILlamaExtractor _llama;
    private readonly IResolverOrchestrator _resolver;
    private readonly IFileSystemService _fs;
    private readonly Serilog.ILogger _logger = Log.ForContext<ProcessService>();
    private readonly MarkdownOptions _mdOptions;

    public ProcessService(
        ITemplateRepository templates,
        IMarkdownConverter converter,
        ILlamaExtractor llama,
        IResolverOrchestrator resolver,
        IFileSystemService fs,
        IOptions<MarkdownOptions> mdOptions)
    {
        _templates = templates;
        _converter = converter;
        _llama = llama;
        _resolver = resolver;
        _fs = fs;
        _mdOptions = mdOptions.Value;
    }

    public async Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
    {
        try
        {
            var tpl = _templates.GetByToken(input.TemplateToken);
            if (tpl == null)
                return new ProcessResult(false, string.Empty, null, "template not found", null, null);

            var fields = JsonSerializer.Deserialize<List<FieldSpec>>(tpl.FieldsJson) ?? new();

            await using var fs = File.OpenRead(input.InputPath);
            var contentType = GetContentType(input.InputPath);

            var totalSw = Stopwatch.StartNew();
            MarkdownResult md;
            var mdSw = Stopwatch.StartNew();
            if (contentType == "application/pdf")
            {
                md = await _converter.ConvertPdfAsync(fs, _mdOptions, ct);
            }
            else
            {
                md = await _converter.ConvertImageAsync(fs, _mdOptions, ct);
            }
            mdSw.Stop();

            await _fs.SaveTextAtomic(input.JobId, Path.GetFileName(input.MarkdownPath), md.Markdown);
            var mdCreated = DateTimeOffset.UtcNow;

            var llmSw = Stopwatch.StartNew();
            var llm = await _llama.ExtractAsync(md.Markdown, tpl.Name, tpl.PromptMarkdown ?? string.Empty, fields, ct);
            llmSw.Stop();

            var fullPrompt = $"[SYSTEM]\n{llm.SystemPrompt}\n\n[USER]\n{llm.UserPrompt}";
            await _fs.SaveTextAtomic(input.JobId, Path.GetFileName(input.PromptPath), fullPrompt);
            var promptCreated = DateTimeOffset.UtcNow;

            var pages = md.Pages.Select(p => new DocumentIndexBuilder.SourcePage(p.Number, (float)p.Width, (float)p.Height)).ToList();
            var words = md.Boxes.Select(b => new DocumentIndexBuilder.SourceWord(b.Page, b.Text, (float)b.XNorm, (float)b.YNorm, (float)b.WidthNorm, (float)b.HeightNorm, false)).ToList();
            var index = DocumentIndexBuilder.Build(pages, words);
            var resolved = await _resolver.ResolveAsync(index, llm.Analysis.Fields, ct);
            var enrichedFields = resolved.Select(r => new ExtractedField(r.FieldName, r.Value, r.Confidence, r.Spans, r.Pointer)).ToList();
            var enriched = new DocumentAnalysisResult(llm.Analysis.DocumentType, enrichedFields, llm.Analysis.Language, llm.Analysis.Notes);

            totalSw.Stop();

            var output = new
            {
                document_type = enriched.DocumentType,
                language = enriched.Language,
                notes = enriched.Notes,
                fields = enriched.Fields.Select(f => new { key = f.Key, value = f.Value, confidence = f.Confidence }),
                metrics = new
                {
                    markdown_ms = mdSw.Elapsed.TotalMilliseconds,
                    llm_ms = llmSw.Elapsed.TotalMilliseconds,
                    total_ms = totalSw.Elapsed.TotalMilliseconds
                }
            };
            var json = JsonSerializer.Serialize(output);
            return new ProcessResult(true, json, md.Markdown, null, mdCreated, promptCreated);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "ProcessFailed {JobId}", input.JobId);
            return new ProcessResult(false, string.Empty, null, ex.Message, null, null);
        }
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
