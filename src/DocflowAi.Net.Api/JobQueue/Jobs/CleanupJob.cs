using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;

namespace DocflowAi.Net.Api.JobQueue.Jobs;

public class CleanupJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<JobQueueOptions> _options;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(IServiceScopeFactory scopeFactory, IOptions<JobQueueOptions> options, ILogger<CleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    [Queue("maintenance")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync()
    {
        if (!_options.Value.Cleanup.Enabled)
            return;
        await RunOnceAsync(CancellationToken.None);
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var sw = Stopwatch.StartNew();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.Value.JobTTLDays);
        _logger.LogInformation("CleanupStarted {Cutoff}", cutoff);
        var removed = store.DeleteOlderThan(cutoff).ToList();
        uow.SaveChanges();
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

