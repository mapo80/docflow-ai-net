using System.Text;
using System.Collections.Generic;
using DocflowAi.Net.Application.Abstractions;
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
        var md = "# Title\n- Key: Value";
        var expected = new DocumentAnalysisResult("test", new List<ExtractedField>{ new("Key","Value",0.9)}, "en", null);
        var mdClient = new Mock<IMarkitdownClient>();
        mdClient.Setup(x => x.ToMarkdownAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(md);
        var llama = new Mock<ILlamaExtractor>();
        llama.Setup(x => x.ExtractAsync(md, "tpl", "prompt", It.IsAny<IReadOnlyList<FieldSpec>>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("fake")), 0, 4, "file", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var orchestrator = new ProcessingOrchestrator(mdClient.Object, llama.Object, new LoggerFactory().CreateLogger<ProcessingOrchestrator>());
        var res = await orchestrator.ProcessAsync(file, "tpl", "prompt", new List<FieldSpec>(), default);
        res.Should().BeEquivalentTo(expected);
        mdClient.VerifyAll(); llama.VerifyAll();
    }
}
