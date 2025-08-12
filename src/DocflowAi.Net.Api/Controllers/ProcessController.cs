using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
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
    public async Task<IActionResult> Process(
        IFormFile file,
        [FromForm] string templateName,
        [FromForm] string prompt,
        [FromForm] IList<string> fields,
        CancellationToken ct)
    {
        if (file is null) return BadRequest("file is required");
        if (string.IsNullOrWhiteSpace(templateName)) return BadRequest("templateName is required");
        if (string.IsNullOrWhiteSpace(prompt)) return BadRequest("prompt is required");
        if (fields is null || fields.Count == 0) return BadRequest("fields are required");

        if (Request.Headers.TryGetValue("X-Reasoning", out var mode))
        {
            var v = mode.ToString().Trim().ToLowerInvariant();
            _modeAccessor.Mode = v switch { "think" => ReasoningMode.Think, "no_think" => ReasoningMode.NoThink, _ => ReasoningMode.Auto };
        }

        var reqFields = new List<FieldSpecDto>();
        foreach (var f in fields)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;
            try
            {
                var dto = JsonSerializer.Deserialize<FieldSpecDto>(f);
                if (dto is not null) reqFields.Add(dto);
                else return BadRequest("fields must be valid JSON");
            }
            catch
            {
                return BadRequest("fields must be valid JSON");
            }
        }

        if (reqFields.Count == 0) return BadRequest("fields must be provided");

        var specs = reqFields.Select(f => new FieldSpec
        {
            Key = f.Key,
            Type = f.Format.ToLowerInvariant() switch
            {
                "int" => "number",
                "double" => "number",
                "date" => "date",
                _ => "string"
            }
        }).ToList();

        var result = await _orchestrator.ProcessAsync(file, templateName, prompt, specs, ct);
        return Ok(result);
    }

    private sealed record FieldSpecDto(string Key, string Format);
}
