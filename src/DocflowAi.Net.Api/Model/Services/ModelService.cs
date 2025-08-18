using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Application.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Linq;
using DocflowAi.Net.Api.Options;

namespace DocflowAi.Net.Api.Model.Services;

/// <summary>
/// Application service for model management.
/// </summary>
public class ModelService : IModelService
{
    private readonly IModelRepository _repo;
    private readonly ILogger<ModelService> _logger;
    private readonly IBackgroundJobClient _jobs;
    private readonly ModelDownloadOptions _options;

    public ModelService(
        IModelRepository repo,
        ILogger<ModelService> logger,
        IBackgroundJobClient jobs,
        IOptions<ModelDownloadOptions> options)
    {
        _repo = repo;
        _logger = logger;
        _jobs = jobs;
        _options = options.Value;
    }

    public IEnumerable<ModelDto> GetAll() => _repo.GetAll().Select(ToDto);

    public ModelDto? GetById(Guid id)
    {
        var model = _repo.GetById(id);
        return model == null ? null : ToDto(model);
    }

    public ModelDto Create(CreateModelRequest request)
    {
        if (_repo.ExistsByName(request.Name))
            throw new InvalidOperationException("model name exists");

        var model = new ModelDocument
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            Provider = request.Provider,
            BaseUrl = request.BaseUrl,
            HfRepo = request.HfRepo,
            ModelFile = request.ModelFile,
            DownloadStatus = request.Type == "local" ? "NotRequested" : null,
            Downloaded = request.Type == "local" ? false : (bool?)null,
            IsActive = true,
        };
        _repo.Add(model, request.ApiKey, request.HfToken);
        _repo.SaveChanges();
        _logger.LogInformation("ModelCreated {Id} {Name}", model.Id, model.Name);
        return ToDto(model);
    }

    public void StartDownload(Guid id)
    {
        var model = _repo.GetById(id) ?? throw new InvalidOperationException("model not found");
        if (model.Type != "local") throw new InvalidOperationException("only local models");
        Directory.CreateDirectory(_options.LogDirectory);
        var logPath = Path.Combine(_options.LogDirectory, $"{id}.log");
        _repo.SetDownloadLogPath(id, logPath);
        _repo.SetDownloadStatus(id, "PendingDownload");
        _repo.SaveChanges();
        _jobs.Enqueue<ModelService>(s => s.RunDownload(id));
    }

    public async Task RunDownload(Guid id)
    {
        var model = _repo.GetById(id);
        if (model == null) return;
        var logPath = model.DownloadLogPath ?? Path.Combine(_options.LogDirectory, $"{id}.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        try
        {
            await File.AppendAllTextAsync(logPath, "Starting\n");
            _repo.SetDownloadStatus(id, "Downloading");
            _repo.SaveChanges();

            var token = _repo.GetHfToken(id);
            if (model.HfRepo == null || model.ModelFile == null)
                throw new InvalidOperationException("missing repo info");
            var url = $"https://huggingface.co/{model.HfRepo}/resolve/main/{model.ModelFile}";
            using var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? 0;
            Directory.CreateDirectory(_options.ModelDirectory);
            var destPath = Path.Combine(_options.ModelDirectory, model.ModelFile);
            await using var src = await response.Content.ReadAsStreamAsync();
            await using var dst = File.Create(destPath);
            var buffer = new byte[81920];
            long read = 0;
            while (true)
            {
                var len = await src.ReadAsync(buffer);
                if (len == 0) break;
                await dst.WriteAsync(buffer.AsMemory(0, len));
                read += len;
                if (total > 0)
                {
                    var pct = (int)(read * 100 / total);
                    await File.AppendAllTextAsync(logPath, $"Progress {pct}%\n");
                }
            }
            _repo.SetDownloaded(id, true, destPath, read);
            _repo.SetDownloadStatus(id, "Downloaded");
            _repo.SaveChanges();
            await File.AppendAllTextAsync(logPath, "Completed\n");
        }
        catch (Exception ex)
        {
            await File.AppendAllTextAsync(logPath, $"Error: {ex.Message}\n");
            _repo.SetDownloadStatus(id, "Failed");
            _repo.SaveChanges();
        }
    }

    public string GetDownloadLog(Guid id)
    {
        var model = _repo.GetById(id);
        if (model?.DownloadLogPath == null || !File.Exists(model.DownloadLogPath)) return string.Empty;
        return File.ReadAllText(model.DownloadLogPath);
    }

    private static ModelDto ToDto(ModelDocument m) =>
        new(m.Id, m.Name, m.Type, m.Provider, m.HfRepo, m.ModelFile, m.Downloaded, m.DownloadStatus, m.LastUsedAt, m.ApiKeyEncrypted != null, m.HfTokenEncrypted != null);
}
