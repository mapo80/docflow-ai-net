using System.Collections.Generic;
using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.SchemeName)]
public sealed class ProcessController : ControllerBase
{
    private readonly IProcessingOrchestrator _orchestrator;
    private readonly IReasoningModeAccessor _modeAccessor;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(IProcessingOrchestrator orchestrator, IReasoningModeAccessor modeAccessor, ILogger<ProcessController> logger)
    {
        _orchestrator = orchestrator;
        _modeAccessor = modeAccessor;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentAnalysisResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Process([FromForm] ProcessRequest request, CancellationToken ct)
    {
        var file = request.File;
        var templateName = request.TemplateName;
        var prompt = request.Prompt;
        var fields = request.Fields;

        _logger.LogInformation("Received processing request: file={FileName} template={Template} fields={FieldCount}", file?.FileName, templateName, fields?.Count);

        if (file is null)
        {
            _logger.LogWarning("Processing request rejected: missing file");
            return BadRequest("file is required");
        }
        if (string.IsNullOrWhiteSpace(templateName))
        {
            _logger.LogWarning("Processing request rejected: missing templateName");
            return BadRequest("templateName is required");
        }
        if (string.IsNullOrWhiteSpace(prompt))
        {
            _logger.LogWarning("Processing request rejected: missing prompt");
            return BadRequest("prompt is required");
        }
        if (fields is null || fields.Count == 0)
        {
            _logger.LogWarning("Processing request rejected: missing fields");
            return BadRequest("fields are required");
        }

        if (Request.Headers.TryGetValue("X-Reasoning", out var mode))
        {
            var v = mode.ToString().Trim().ToLowerInvariant();
            _modeAccessor.Mode = v switch { "think" => ReasoningMode.Think, "no_think" => ReasoningMode.NoThink, _ => ReasoningMode.Auto };
            _logger.LogInformation("Reasoning mode header detected: {Mode}", _modeAccessor.Mode);
        }

        var specs = new List<FieldSpec>();
        foreach (var f in fields)
        {
            if (string.IsNullOrWhiteSpace(f.FieldName))
            {
                _logger.LogWarning("Processing request rejected: field with empty name");
                return BadRequest("fieldName is required");
            }
            var type = f.Format?.ToLowerInvariant() switch
            {
                "int" => "number",
                "double" => "number",
                "date" => "date",
                _ => "string"
            };
            specs.Add(new FieldSpec { Key = f.FieldName, Type = type });
        }

        _logger.LogInformation("Invoking orchestrator for {File} with {Specs} specs", file.FileName, specs.Count);
        var result = await _orchestrator.ProcessAsync(file, templateName, prompt, specs, ct);
        _logger.LogInformation("Orchestrator completed for {File}", file.FileName);
        return Ok(result);
    }

}
