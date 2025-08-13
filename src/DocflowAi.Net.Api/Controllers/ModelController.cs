using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Controllers;

[ApiController]
[Route("api/v1/model")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.SchemeName)]
public sealed class ModelController : ControllerBase
{
    private readonly ILlmModelService _service;

    public ModelController(ILlmModelService service)
    {
        _service = service;
    }

    [HttpPost("switch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Switch([FromBody] SwitchModelRequest request, CancellationToken ct)
    {
        await _service.SwitchModelAsync(request.HfKey, request.ModelRepo, request.ModelFile, request.ContextSize, ct);
        return Ok();
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ModelDownloadStatus), StatusCodes.Status200OK)]
    public ActionResult<ModelDownloadStatus> Status()
    {
        return Ok(_service.GetStatus());
    }
}
