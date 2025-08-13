using System.Text;
using System.Collections.Generic;
using System.IO;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Infrastructure.Orchestration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
namespace DocflowAi.Net.Tests.Unit;
public class ProcessingOrchestratorTests {
    [Fact] public async Task ProcessAsync_InvokesServices_AndReturnsResult() {
        var md = new MarkdownResult("# Title\n- Key: Value", new List<PageInfo>(), new List<Box>());
        var expected = new DocumentAnalysisResult("test", new List<ExtractedField>{ new("Key","Value",0.9)}, "en", null);
        var mdClient = new Mock<IMarkdownConverter>();
        mdClient.Setup(x => x.ConvertImageAsync(It.IsAny<Stream>(), It.IsAny<MarkdownOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(md);
        var llama = new Mock<ILlamaExtractor>();
        llama.Setup(x => x.ExtractAsync(md.Markdown, "tpl", "prompt", It.IsAny<IReadOnlyList<FieldSpec>>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("fake")), 0, 4, "file", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var orchestrator = new ProcessingOrchestrator(mdClient.Object, llama.Object, new LoggerFactory().CreateLogger<ProcessingOrchestrator>());
        var res = await orchestrator.ProcessAsync(file, "tpl", "prompt", new List<FieldSpec>(), default);
        res.Should().BeEquivalentTo(expected);
        mdClient.VerifyAll(); llama.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_Overload_MapsFieldNames()
    {
        var md = new MarkdownResult("# Title\n- Key: Value", new List<PageInfo>(), new List<Box>());
        var expected = new DocumentAnalysisResult("test", new List<ExtractedField>{ new("Key","Value",0.9)}, "en", null);
        var mdClient = new Mock<IMarkdownConverter>();
        mdClient.Setup(x => x.ConvertImageAsync(It.IsAny<Stream>(), It.IsAny<MarkdownOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(md);
        var llama = new Mock<ILlamaExtractor>();
        llama.Setup(x => x.ExtractAsync(md.Markdown, "tpl", "prompt", It.IsAny<IReadOnlyList<FieldSpec>>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("fake")), 0, 4, "file", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var orchestrator = new ProcessingOrchestrator(mdClient.Object, llama.Object, new LoggerFactory().CreateLogger<ProcessingOrchestrator>());
        var res = await orchestrator.ProcessAsync(file, "tpl", "prompt", new List<string> { "Key" }, default);
        res.Should().BeEquivalentTo(expected);
    }
}
