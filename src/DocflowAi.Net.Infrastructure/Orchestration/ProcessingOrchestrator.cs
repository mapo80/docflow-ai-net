using System.Linq;
using System.IO;
using System.Text.Json;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.BBoxResolver;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Infrastructure.Orchestration;

public sealed class ProcessingOrchestrator : IProcessingOrchestrator
{
    private readonly IMarkdownConverter _converter;
    private readonly ILlamaExtractor _llama;
    private readonly IBBoxResolver _resolver;
    private readonly ILogger<ProcessingOrchestrator> _logger;

    public ProcessingOrchestrator(
        IMarkdownConverter converter,
        ILlamaExtractor llama,
        IBBoxResolver resolver,
        ILogger<ProcessingOrchestrator> logger)
    {
        _converter = converter;
        _llama = llama;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<DocumentAnalysisResult> ProcessAsync(
        IFormFile file,
        string templateName,
        string prompt,
        IReadOnlyList<FieldSpec> fields,
        CancellationToken ct)
    {
        if (file.Length == 0)
            throw new ArgumentException("Empty file.");

        _logger.LogInformation(
            "Starting processing: {FileName} ({Size} bytes, {ContentType}) template={Template} fields={FieldCount}",
            file.FileName,
            file.Length,
            file.ContentType,
            templateName,
            fields.Count);

        await using var stream = file.OpenReadStream();

        var debugDir = Environment.GetEnvironmentVariable("DEBUG_DIR");
        if (!string.IsNullOrWhiteSpace(debugDir))
        {
            Directory.CreateDirectory(debugDir);
            File.WriteAllText(Path.Combine(debugDir, "fields.txt"), JsonSerializer.Serialize(fields));
            File.WriteAllText(Path.Combine(debugDir, "prompt.txt"), prompt);
        }

        try
        {
            MarkdownResult mdResult;
            var options = new MarkdownOptions();
            if (file.ContentType == "application/pdf" || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                mdResult = await _converter.ConvertPdfAsync(stream, options);
            else if (file.ContentType.StartsWith("image/") || new[]{".png",".jpg",".jpeg"}.Any(e => file.FileName.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                mdResult = await _converter.ConvertImageAsync(stream, options);
            else
                throw new ArgumentException($"Unsupported content type: {file.ContentType}");

            if (!string.IsNullOrWhiteSpace(debugDir))
            {
                File.WriteAllText(Path.Combine(debugDir, "markdown.txt"), mdResult.Markdown);
            }

            var result = await _llama.ExtractAsync(mdResult.Markdown, templateName, prompt, fields, ct);
            var pagesIn = mdResult.Pages.Select(p => new DocumentIndexBuilder.SourcePage(p.Number, (float)p.Width, (float)p.Height)).ToList();
            var wordsIn = mdResult.Boxes.Select(b => new DocumentIndexBuilder.SourceWord(b.Page, b.Text, (float)b.XNorm, (float)b.YNorm, (float)b.WidthNorm, (float)b.HeightNorm, false)).ToList();
            var index = DocumentIndexBuilder.Build(pagesIn, wordsIn);
            var resolved = await _resolver.ResolveAsync(index, result.Fields, ct);
            var enrichedFields = resolved.Select(r => new ExtractedField(r.FieldName, r.Value, r.Confidence, r.Spans)).ToList();
            var enriched = new DocumentAnalysisResult(result.DocumentType, enrichedFields, result.Language, result.Notes);

            _logger.LogInformation(
                "Processing completed: {FileName}. Extracted {Count} fields.",
                file.FileName,
                enriched.Fields.Count);

            return enriched;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Processing failed: {FileName}", file.FileName);
            throw;
        }
    }

    public Task<DocumentAnalysisResult> ProcessAsync(
        IFormFile file,
        string templateName,
        string prompt,
        IReadOnlyList<string> fieldNames,
        CancellationToken ct)
    {
        var specs = fieldNames.Select(fn => new FieldSpec { Key = fn, Type = "string" }).ToList();
        return ProcessAsync(file, templateName, prompt, specs, ct);
    }
}

