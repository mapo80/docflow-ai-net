using DocflowAi.Net.Api.Controllers;
using DocflowAi.Net.Api.Models;
using DocflowAi.Net.Application.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocflowAi.Net.Tests.Unit;

public class ModelControllerTests
{
    [Fact]
    public async Task Switch_InvokesService_ReturnsOk()
    {
        var svc = new Mock<ILlmModelService>();
        var controller = new ModelController(svc.Object);
        var req = new SwitchModelRequest { HfKey = "k", ModelRepo = "r", ModelFile = "f", ContextSize = 10 };

        var res = await controller.Switch(req, default);

        svc.Verify(x => x.SwitchModelAsync("k", "r", "f", 10, It.IsAny<CancellationToken>()), Times.Once);
        res.Should().BeOfType<OkResult>();
    }

    [Fact]
    public void Status_ReturnsServiceResult()
    {
        var svc = new Mock<ILlmModelService>();
        var status = new ModelDownloadStatus(true, 100);
        svc.Setup(x => x.GetStatus()).Returns(status);
        var controller = new ModelController(svc.Object);

        var res = controller.Status();

        res.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)res.Result).Value.Should().Be(status);
    }
}
