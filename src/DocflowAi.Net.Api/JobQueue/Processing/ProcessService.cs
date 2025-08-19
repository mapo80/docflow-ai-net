using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Executes the document processing pipeline for a job by selecting the
/// appropriate model backend. Local models run the full extraction pipeline
/// while hosted models are forwarded to the dispatcher.
/// </summary>
public class ProcessService : IProcessService
{
    private readonly IModelDispatchService _dispatcher;
    private readonly IModelRepository _models;
    private readonly ITemplateRepository _templates;
    private readonly IProcessingOrchestrator _orchestrator;
    private readonly Serilog.ILogger _logger = Log.ForContext<ProcessService>();

    public ProcessService(
        IModelDispatchService dispatcher,
        IModelRepository models,
        ITemplateRepository templates,
        IProcessingOrchestrator orchestrator)
    {
        _dispatcher = dispatcher;
        _models = models;
        _templates = templates;
        _orchestrator = orchestrator;
    }

    public async Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var fileInfo = new FileInfo(input.InputPath);
            _logger.Information(
                "ProcessStarted {JobId} {Bytes} {FileExt} {Template} {Model}",
                input.JobId,
                fileInfo.Length,
                fileInfo.Extension,
                input.TemplateToken,
                input.Model);

            var model = _models.GetByName(input.Model)
                ?? throw new InvalidOperationException($"Model '{input.Model}' not found");

            if (model.Type == "local")
            {
                var template = _templates.GetByToken(input.TemplateToken)
                    ?? throw new InvalidOperationException($"Template '{input.TemplateToken}' not found");
                var fields = JsonSerializer.Deserialize<List<FieldSpec>>(template.FieldsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<FieldSpec>();
                await using var fs = File.OpenRead(input.InputPath);
                var contentType = GetContentType(fileInfo.Extension);
                var formFile = new FormFile(fs, 0, fs.Length, "file", fileInfo.Name)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = contentType
                };
                var result = await _orchestrator.ProcessAsync(formFile, template.Token,
                    template.PromptMarkdown ?? string.Empty, fields, ct);
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                sw.Stop();
                _logger.Information("ProcessCompleted {JobId} {ElapsedMs}", input.JobId, sw.ElapsedMilliseconds);
                return new ProcessResult(true, json, null);
            }
            else
            {
                var payload = JsonSerializer.Serialize(new { template = input.TemplateToken });
                var json = await _dispatcher.InvokeAsync(input.Model, payload, ct);
                sw.Stop();
                _logger.Information("ProcessCompleted {JobId} {ElapsedMs}", input.JobId, sw.ElapsedMilliseconds);
                return new ProcessResult(true, json, null);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("ProcessCancelled {JobId}", input.JobId);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.Error(ex, "ProcessFailed {JobId} {ElapsedMs}", input.JobId, sw.ElapsedMilliseconds);
            return new ProcessResult(false, string.Empty, ex.Message);
        }
    }

    private static string GetContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
