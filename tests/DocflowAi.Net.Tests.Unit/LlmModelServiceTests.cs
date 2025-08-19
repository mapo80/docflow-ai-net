using System.Net;
using System.Net.Http;
using DocflowAi.Net.Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using FluentAssertions;

namespace DocflowAi.Net.Tests.Unit;

public class LlmModelServiceTests
{
    [Fact]
    public void Ctor_PicksFirstModel_WhenFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);
        var dest = Path.Combine(tempDir, "file.gguf");
        File.WriteAllBytes(dest, new byte[] {1});

        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var svc = new LlmModelService(new HttpClient(new ThrowHandler()), logger);

        var info = svc.GetCurrentModel();
        info.File.Should().Be("file.gguf");
        info.ContextSize.Should().BeNull();
        info.LoadedAt.Should().NotBeNull();

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SwitchModelAsync_SetsCurrentModel_WhenFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);
        var dest = Path.Combine(tempDir, "file.bin");
        await File.WriteAllBytesAsync(dest, new byte[] {1});

        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var client = new HttpClient(new ThrowHandler());
        var svc = new LlmModelService(client, logger);

        await svc.SwitchModelAsync("file.bin", 5);

        var info = svc.GetCurrentModel();
        info.File.Should().Be("file.bin");
        info.ContextSize.Should().Be(5);
        var status = svc.GetStatus();
        status.Completed.Should().BeTrue();
        status.Percentage.Should().Be(100);

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DownloadModelAsync_ReportsProgressUntilCompleted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);

        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var handler = new SlowHandler();
        var client = new HttpClient(handler);
        var svc = new LlmModelService(client, logger);

        await svc.DownloadModelAsync("k", "repo", "file.bin", CancellationToken.None);

        var initial = svc.GetStatus();
        initial.Completed.Should().BeFalse();
        initial.Percentage.Should().Be(0);

        handler.Complete(new byte[] {1,2,3,4});
        await WaitForCompletionAsync(svc);

        var expectedPath = Path.Combine(tempDir, "file.bin");
        File.Exists(expectedPath).Should().BeTrue();
        var finalStatus = svc.GetStatus();
        finalStatus.Completed.Should().BeTrue();
        finalStatus.Percentage.Should().Be(100);

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DownloadModelAsync_Throws_WhenNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Environment.SetEnvironmentVariable("MODELS_DIR", tempDir);

        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlmModelService>();
        var handler = new NotFoundHandler();
        var client = new HttpClient(handler);
        var svc = new LlmModelService(client, logger);

        await Assert.ThrowsAsync<FileNotFoundException>(() => svc.DownloadModelAsync("k", "repo", "file.bin", CancellationToken.None));

        Environment.SetEnvironmentVariable("MODELS_DIR", null);
        Directory.Delete(tempDir, true);
    }

    private static async Task WaitForCompletionAsync(LlmModelService svc)
    {
        for (var i = 0; i < 50; i++)
        {
            if (svc.GetStatus().Completed)
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException();
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("No HTTP calls expected");
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<byte[]> _tcs = new();
        public void Complete(byte[] content) => _tcs.SetResult(content);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Head)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Array.Empty<byte>())
                };
                resp.Content.Headers.ContentLength = 4;
                return Task.FromResult(resp);
            }

            return _tcs.Task.ContinueWith(t =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(t.Result)
                };
                return resp;
            }, cancellationToken);
        }
    }

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
