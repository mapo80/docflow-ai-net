using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;

namespace DocflowAi.Net.Api.JobQueue.Hosted;

public class CleanupService : BackgroundService
{
    private readonly IJobStore _store;
    private readonly IOptions<JobQueueOptions> _options;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IJobStore store, IOptions<JobQueueOptions> options, ILogger<CleanupService> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = _options.Value.Cleanup;
        if (!cfg.Enabled)
            return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var next = new DateTimeOffset(now.Year, now.Month, now.Day, cfg.DailyHour, cfg.DailyMinute, 0, TimeSpan.Zero);
            if (next <= now) next = next.AddDays(1);
            var delay = next - now;
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }
            await RunOnceAsync(stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.Value.JobTTLDays);
        _logger.LogInformation("CleanupStarted {Cutoff}", cutoff);
        var removed = _store.DeleteOlderThan(cutoff).ToList();
        _logger.LogInformation("CleanupDbDeleted {DeletedCount}", removed.Count);
        foreach (var doc in removed)
        {
            var dir = doc.Paths.Dir;
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    _logger.LogInformation("CleanupFsDeleted {JobId} {Path}", doc.Id, dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CleanupFsError {JobId} {Path}", doc.Id, dir);
            }
        }
        _logger.LogInformation("CleanupCompleted {ElapsedMs}", sw.ElapsedMilliseconds);
        await Task.CompletedTask;
    }
}
