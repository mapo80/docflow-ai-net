using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace DocflowAi.Net.Infrastructure.Llm;

public sealed class LlmModelService : ILlmModelService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmModelService> _logger;

    private Task? _downloadTask;
    private long _totalBytes;
    private long _downloadedBytes;
    private ModelInfo _currentModel;

    public LlmModelService(HttpClient httpClient, IOptions<LlmOptions> options, ILogger<LlmModelService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(modelsDir);

        string? fileName = null;
        var configured = _options.ModelPath;
        if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
        {
            fileName = Path.GetFileName(configured);
        }
        else
        {
            var first = Directory.EnumerateFiles(modelsDir, "*.gguf").FirstOrDefault();
            if (first != null)
            {
                _options.ModelPath = first;
                fileName = Path.GetFileName(first);
                _logger.LogInformation("Initial model set to {ModelPath}", first);
            }
        }

        _currentModel = new ModelInfo(null, null, fileName, _options.ContextTokens, fileName != null ? DateTime.UtcNow : null);
    }

    public async Task DownloadModelAsync(string hfKey, string modelRepo, string modelFile, CancellationToken ct)
    {
        if (_downloadTask != null && !_downloadTask.IsCompleted)
            throw new InvalidOperationException("download in progress");

        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(modelsDir);
        var dest = Path.Combine(modelsDir, modelFile);
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
        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        var dest = Path.Combine(modelsDir, modelFile);
        if (!File.Exists(dest))
            throw new FileNotFoundException($"Model {modelFile} not found");
        _options.ModelPath = dest;
        _options.ContextTokens = contextSize;
        _currentModel = new ModelInfo(null, null, modelFile, contextSize, DateTime.UtcNow);
        _logger.LogInformation("LLM model switched to {ModelPath}", dest);
        return Task.CompletedTask;
    }

    public IEnumerable<string> ListAvailableModels()
    {
        var modelsDir = Environment.GetEnvironmentVariable("MODELS_DIR") ?? Path.Combine(AppContext.BaseDirectory, "models");
        if (!Directory.Exists(modelsDir)) return Enumerable.Empty<string>();
        return Directory.EnumerateFiles(modelsDir, "*.gguf").Select(Path.GetFileName) ?? Enumerable.Empty<string>();
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
