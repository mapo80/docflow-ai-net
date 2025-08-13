using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlmModelService : ILlmModelService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmModelService> _logger;

    private Task? _downloadTask;
    private long _totalBytes;
    private long _downloadedBytes;

    public LlmModelService(HttpClient httpClient, IOptions<LlmOptions> options, ILogger<LlmModelService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SwitchModelAsync(string hfKey, string modelRepo, string modelFile, int contextSize, CancellationToken ct)
    {
        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(modelsDir);
        var dest = Path.Combine(modelsDir, modelFile);
        if (File.Exists(dest))
        {
            _options.ModelPath = dest;
            _options.ContextTokens = contextSize;
            _logger.LogInformation("LLM model switched to {ModelPath}", dest);
            _downloadTask = null;
            _totalBytes = 0;
            _downloadedBytes = 0;
            return;
        }

        var url = $"https://huggingface.co/{modelRepo}/resolve/main/{modelFile}";

        using var head = new HttpRequestMessage(HttpMethod.Head, url);
        head.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfKey);
        using var headResp = await _httpClient.SendAsync(head, ct);
        if (headResp.StatusCode == HttpStatusCode.NotFound)
            throw new FileNotFoundException($"Model {modelRepo}/{modelFile} not found on HuggingFace");
        headResp.EnsureSuccessStatusCode();
        _totalBytes = headResp.Content.Headers.ContentLength ?? 0;
        _downloadedBytes = 0;

        _downloadTask = DownloadAndSwitchAsync(url, hfKey, dest, contextSize, ct);
    }

    private async Task DownloadAndSwitchAsync(string url, string hfKey, string dest, int contextSize, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfKey);
            using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            await using var fs = File.Create(dest);
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                Interlocked.Add(ref _downloadedBytes, read);
            }

            _options.ModelPath = dest;
            _options.ContextTokens = contextSize;
            _logger.LogInformation("LLM model switched to {ModelPath}", dest);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Error downloading model {Url}", url);
        }
    }

    public ModelDownloadStatus GetStatus()
    {
        var task = _downloadTask;
        if (task == null)
            return new ModelDownloadStatus(true, 100);
        var completed = task.IsCompleted;
        var pct = _totalBytes > 0 ? (double)Interlocked.Read(ref _downloadedBytes) / _totalBytes * 100 : 0;
        if (completed) pct = 100;
        return new ModelDownloadStatus(completed, pct);
    }
}
