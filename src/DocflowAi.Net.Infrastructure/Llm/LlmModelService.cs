using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlmModelService : ILlmModelService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmModelService> _logger;
    private readonly string _modelsDir;

    private Task? _downloadTask;
    private long _totalBytes;
    private long _downloadedBytes;
    private ModelInfo _currentModel;

    public LlmModelService(HttpClient httpClient, ILogger<LlmModelService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(_modelsDir);

        var first = Directory.EnumerateFiles(_modelsDir, "*.gguf").FirstOrDefault();
        var fileName = first != null ? Path.GetFileName(first) : null;
        _currentModel = new ModelInfo(null, null, fileName, null, fileName != null ? DateTime.UtcNow : null);
    }

    public async Task DownloadModelAsync(string hfKey, string modelRepo, string modelFile, CancellationToken ct)
    {
        if (_downloadTask != null && !_downloadTask.IsCompleted)
            throw new InvalidOperationException("download in progress");

        Directory.CreateDirectory(_modelsDir);
        var dest = Path.Combine(_modelsDir, modelFile);
        if (File.Exists(dest))
        {
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

        _downloadTask = DownloadAsync(url, hfKey, dest, ct);
    }

    public Task SwitchModelAsync(string modelFile, int contextSize)
    {
        var dest = Path.Combine(_modelsDir, modelFile);
        if (!File.Exists(dest))
            throw new FileNotFoundException($"Model {modelFile} not found");
        _currentModel = new ModelInfo(null, null, modelFile, contextSize, DateTime.UtcNow);
        _logger.LogInformation("LLM model switched to {ModelPath}", dest);
        return Task.CompletedTask;
    }

    public IEnumerable<string> ListAvailableModels()
    {
        if (!Directory.Exists(_modelsDir)) return Enumerable.Empty<string>();
        return Directory.EnumerateFiles(_modelsDir, "*.gguf").Select(f => Path.GetFileName(f)!);
    }

    private async Task DownloadAsync(string url, string hfKey, string dest, CancellationToken ct)
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

    public ModelInfo GetCurrentModel() => _currentModel;
}
