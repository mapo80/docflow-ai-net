using System.Collections.Generic;
using System.Text;
using DocflowAi.Net.Api.Controllers;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocflowAi.Net.Tests.Unit;

public class ProcessControllerHeaderTests
{
    [Fact]
    public async Task Process_SetsReasoningMode_FromHeader()
    {
        var orchestrator = new Mock<IProcessingOrchestrator>();
        orchestrator
            .Setup(o => o.ProcessAsync(
                It.IsAny<IFormFile>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<FieldSpec>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocflowAi.Net.Domain.Extraction.DocumentAnalysisResult("t", new List<DocflowAi.Net.Domain.Extraction.ExtractedField>(), "en", null));

        var accessor = new Mock<IReasoningModeAccessor>();
        accessor.SetupProperty(a => a.Mode, ReasoningMode.Auto);

        var controller = new ProcessController(orchestrator.Object, accessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        controller.HttpContext.Request.Headers["X-Reasoning"] = "no_think";

        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("fake")), 0, 4, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var fields = new List<string> { "{\"Key\":\"a\",\"Format\":\"string\"}" };
        var res = await controller.Process(file, "tpl", "prompt", fields, default);

        accessor.Object.Mode.Should().Be(ReasoningMode.NoThink);
        res.Should().BeOfType<OkObjectResult>();
    }
}

