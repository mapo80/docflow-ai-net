using System.Net;
using System.Net.Http;
using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Infrastructure.Llm;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Tests.Unit;

public class LlmModelServiceTests
{
    private static HttpClient CreateClient(HttpStatusCode status, byte[] content)
    {
        var handler = new MockHttpMessageHandler(status, content);
        return new HttpClient(handler);
    }

    [Fact]
    public async Task SwitchModelAsync_DownloadsAndUpdatesOptions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);

        var opts = Options.Create(new LlmOptions { ModelPath = "default", ContextTokens = 1 });
        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var client = CreateClient(HttpStatusCode.OK, new byte[] {1,2,3});
        var svc = new LlmModelService(client, opts, logger);

        await svc.SwitchModelAsync("k", "repo", "file.bin", 2048, CancellationToken.None);

        var expectedPath = Path.Combine(tempDir, "file.bin");
        File.Exists(expectedPath).Should().BeTrue();
        opts.Value.ModelPath.Should().Be(expectedPath);
        opts.Value.ContextTokens.Should().Be(2048);

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SwitchModelAsync_Throws_WhenNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);

        var opts = Options.Create(new LlmOptions { ModelPath = "default", ContextTokens = 1 });
        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var client = CreateClient(HttpStatusCode.NotFound, Array.Empty<byte>());
        var svc = new LlmModelService(client, opts, logger);

        await Assert.ThrowsAsync<FileNotFoundException>(() => svc.SwitchModelAsync("k", "repo", "file.bin", 10, CancellationToken.None));

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly byte[] _content;
        public MockHttpMessageHandler(HttpStatusCode status, byte[] content)
        {
            _status = status;
            _content = content;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_status)
            {
                Content = new ByteArrayContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
