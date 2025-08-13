using System.Collections.Generic;
using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.SchemeName)]
public sealed class ProcessController : ControllerBase
{
    private readonly IProcessingOrchestrator _orchestrator;
    private readonly IReasoningModeAccessor _modeAccessor;

    public ProcessController(IProcessingOrchestrator orchestrator, IReasoningModeAccessor modeAccessor)
    {
        _orchestrator = orchestrator;
        _modeAccessor = modeAccessor;
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

        if (file is null) return BadRequest("file is required");
        if (string.IsNullOrWhiteSpace(templateName)) return BadRequest("templateName is required");
        if (string.IsNullOrWhiteSpace(prompt)) return BadRequest("prompt is required");
        if (fields is null || fields.Count == 0) return BadRequest("fields are required");

        if (Request.Headers.TryGetValue("X-Reasoning", out var mode))
        {
            var v = mode.ToString().Trim().ToLowerInvariant();
            _modeAccessor.Mode = v switch { "think" => ReasoningMode.Think, "no_think" => ReasoningMode.NoThink, _ => ReasoningMode.Auto };
        }

        var specs = new List<FieldSpec>();
        foreach (var f in fields)
        {
            if (string.IsNullOrWhiteSpace(f.FieldName)) return BadRequest("fieldName is required");
            var type = f.Format?.ToLowerInvariant() switch
            {
                "int" => "number",
                "double" => "number",
                "date" => "date",
                _ => "string"
            };
            specs.Add(new FieldSpec { Key = f.FieldName, Type = type });
        }

        var result = await _orchestrator.ProcessAsync(file, templateName, prompt, specs, ct);
        return Ok(result);
    }

}
