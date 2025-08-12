using System.Linq;
using System.IO;
using System.Text.Json;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Infrastructure.Orchestration;

public sealed class ProcessingOrchestrator : IProcessingOrchestrator
{
    private readonly IMarkitdownClient _markitdown;
    private readonly ILlamaExtractor _llama;
    private readonly ILogger<ProcessingOrchestrator> _logger;

    public ProcessingOrchestrator(
        IMarkitdownClient markitdown,
        ILlamaExtractor llama,
        ILogger<ProcessingOrchestrator> logger)
    {
        _markitdown = markitdown;
        _llama = llama;
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
            var markdown = await _markitdown.ToMarkdownAsync(stream, file.FileName, ct);
            if (!string.IsNullOrWhiteSpace(debugDir))
                File.WriteAllText(Path.Combine(debugDir, "markitdown.txt"), markdown);

            var result = await _llama.ExtractAsync(markdown, templateName, prompt, fields, ct);

            _logger.LogInformation(
                "Processing completed: {FileName}. Extracted {Count} fields.",
                file.FileName,
                result.Fields.Count);

            return result;
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

