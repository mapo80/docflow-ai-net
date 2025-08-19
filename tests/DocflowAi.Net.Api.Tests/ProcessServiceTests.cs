using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Api.Templates.Models;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.BBoxResolver;
using DocflowAi.Net.Domain.Extraction;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class ProcessServiceTests
{
    private static JsonSerializerOptions JsonOpts => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task ExecuteAsync_LocalModel_Uses_Orchestrator()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        var models = Substitute.For<IModelRepository>();
        models.GetByName("m").Returns(new ModelDocument { Name = "m", Type = "local" });
        var templates = Substitute.For<ITemplateRepository>();
        templates.GetByToken("t").Returns(new TemplateDocument { Token = "t", FieldsJson = "[{\"key\":\"a\"}]", PromptMarkdown = "p" });
        var orchestrator = Substitute.For<IProcessingOrchestrator>();
        var expected = new DocumentAnalysisResult("dt", new List<ExtractedField>(), "en", null);
        orchestrator.ProcessAsync(Arg.Any<IFormFile>(), "t", "p", Arg.Any<IReadOnlyList<FieldSpec>>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var svc = new ProcessService(dispatcher, models, templates, orchestrator);
        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "data");
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        var result = await svc.ExecuteAsync(input, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputJson.Should().Be(JsonSerializer.Serialize(expected, JsonOpts));
        await dispatcher.DidNotReceive().InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_RemoteModel_Returns_Dispatcher_Result()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        dispatcher.InvokeAsync("m", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("out");
        var models = Substitute.For<IModelRepository>();
        models.GetByName("m").Returns(new ModelDocument { Name = "m", Type = "hosted-llm" });
        var templates = Substitute.For<ITemplateRepository>();
        var orchestrator = Substitute.For<IProcessingOrchestrator>();
        var svc = new ProcessService(dispatcher, models, templates, orchestrator);
        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "data");
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        var result = await svc.ExecuteAsync(input, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputJson.Should().Be("out");
        await orchestrator.DidNotReceive().ProcessAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FieldSpec>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Propagates_Cancellation()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        var models = Substitute.For<IModelRepository>();
        models.GetByName("m").Returns(new ModelDocument { Name = "m", Type = "local" });
        var templates = Substitute.For<ITemplateRepository>();
        templates.GetByToken("t").Returns(new TemplateDocument { Token = "t", FieldsJson = "[]" });
        var orchestrator = Substitute.For<IProcessingOrchestrator>();
        orchestrator.ProcessAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FieldSpec>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<DocumentAnalysisResult>(new OperationCanceledException()));
        var svc = new ProcessService(dispatcher, models, templates, orchestrator);
        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "data");
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        await Assert.ThrowsAsync<OperationCanceledException>(() => svc.ExecuteAsync(input, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Returns_Failure_On_Exception()
    {
        var dispatcher = Substitute.For<IModelDispatchService>();
        var models = Substitute.For<IModelRepository>();
        models.GetByName("m").Returns(new ModelDocument { Name = "m", Type = "local" });
        var templates = Substitute.For<ITemplateRepository>();
        templates.GetByToken("t").Returns(new TemplateDocument { Token = "t", FieldsJson = "[]" });
        var orchestrator = Substitute.For<IProcessingOrchestrator>();
        orchestrator.ProcessAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FieldSpec>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<DocumentAnalysisResult>(new InvalidOperationException("boom")));
        var svc = new ProcessService(dispatcher, models, templates, orchestrator);
        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "data");
        var input = new ProcessInput(Guid.NewGuid(), temp, "t", "m");

        var result = await svc.ExecuteAsync(input, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
    }
}
