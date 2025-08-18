using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Features.Models;

public class ModelDownloadWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ModelDownloadWorker> _log;
    private readonly ConcurrentQueue<Guid> _queue = new();

    public ModelDownloadWorker(IServiceProvider sp, ILogger<ModelDownloadWorker> log)
    {
        _sp = sp; _log = log;
    }

    public void Enqueue(Guid id) => _queue.Enqueue(id);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("ModelDownloadWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_queue.TryDequeue(out var id))
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
                var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var token = cfg["HF_TOKEN"];
                if (!string.IsNullOrWhiteSpace(token))
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var downloaders = scope.ServiceProvider.GetRequiredService<IEnumerable<Downloaders.IModelDownloader>>();

                var model = await db.Models.FirstOrDefaultAsync(m => m.Id == id, stoppingToken);
                if (model is null) continue;

                if (model.Status == ModelStatus.Downloading)
                {
                    _log.LogWarning("Model {Id} is already downloading", id);
                    continue;
                }

                model.Status = ModelStatus.Downloading;
                model.ErrorMessage = null;
                model.DownloadProgress = 0;
                await db.SaveChangesAsync(stoppingToken);

                var storageRoot = scope.ServiceProvider.GetRequiredService<IConfiguration>()
                    .GetSection("ModelStorage").GetValue<string>("Root") ?? "models";
                Directory.CreateDirectory(storageRoot);

                var fileName = model.SourceType switch
                {
                    ModelSourceType.Local => Path.GetFileName(model.LocalPath) ?? $"{model.Name}.gguf",
                    ModelSourceType.Url => Path.GetFileName(new Uri(model.Url!).LocalPath),
                    ModelSourceType.HuggingFace => model.HfFilename ?? $"{model.Name}.gguf",
                    _ => $"{model.Name}.gguf"
                };
                var targetPath = Path.Combine(storageRoot, fileName);

                var downloader = downloaders.FirstOrDefault(d => d.CanHandle(model));
                if (downloader is null && model.SourceType == ModelSourceType.Local && model.LocalPath is { Length: > 0 })
                {
                    // "Download" from local simply means ensuring file exists and copying if needed
                    if (!File.Exists(model.LocalPath))
                        throw new InvalidOperationException("Local path not found");
                    if (!Path.GetFullPath(model.LocalPath).Equals(Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(model.LocalPath, targetPath, overwrite: true);
                    }
                    model.LocalPath = targetPath;
                    model.FileSize = new FileInfo(targetPath).Length;
                    model.Status = ModelStatus.Available;
                    model.DownloadProgress = 100;
                    await db.SaveChangesAsync(stoppingToken);
                    continue;
                }
                else if (downloader is null)
                {
                    throw new InvalidOperationException("No downloader available for the model source");
                }

                var progress = new Progress<int>(async pct =>
                {
                    var m = await db.Models.FirstAsync(x => x.Id == model.Id, stoppingToken);
                    m.DownloadProgress = pct;
                    await db.SaveChangesAsync(stoppingToken);
                });

                await downloader.DownloadAsync(model, targetPath, progress, stoppingToken);

                var fi = new FileInfo(targetPath);
                model.LocalPath = targetPath;
                model.FileSize = fi.Exists ? fi.Length : null;
                model.Status = ModelStatus.Available;
                model.DownloadProgress = 100;
                await db.SaveChangesAsync(stoppingToken);

                _log.LogInformation("Model downloaded: {Name} -> {Path}", model.Name, targetPath);
            }
            catch (Exception ex)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
                    if (_queue.TryPeek(out var idForErr))
                    {
                        var m = await db.Models.FirstOrDefaultAsync(m => m.Id == idForErr, stoppingToken);
                        if (m is not null)
                        {
                            m.Status = ModelStatus.Error;
                            m.ErrorMessage = ex.Message;
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch { /* ignore nested errors */ }

                _log.LogError(ex, "Error during model download");
            }
        }
    }
}
