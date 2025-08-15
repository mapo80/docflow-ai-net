using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using Hangfire;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace DocflowAi.Net.Api.JobQueue.Hosted;

public class ReschedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundJobClient _jobs;
    private readonly IFileSystemService _fs;
    private readonly JobQueueOptions _options;
    private readonly Serilog.ILogger _logger = Log.ForContext<ReschedulerService>();

    public ReschedulerService(IServiceScopeFactory scopeFactory, IBackgroundJobClient jobs, IFileSystemService fs, IOptions<JobQueueOptions> opts)
    {
        _scopeFactory = scopeFactory;
        _jobs = jobs;
        _fs = fs;
        _options = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOnceAsync(stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    public Task TickAsync(CancellationToken ct) => ProcessOnceAsync(ct);

    public async Task ProcessOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = DateTimeOffset.UtcNow;

        foreach (var job in store.FindQueuedDue(now))
        {
            _logger.Information("RescheduleEnqueue {JobId}", job.Id);
            _jobs.Enqueue<IJobRunner>(r => r.Run(job.Id, CancellationToken.None, true, null));
        }

        foreach (var job in store.FindRunningExpired(now))
        {
            var attempt = job.Attempts + 1;
            if (attempt <= _options.Queue.MaxAttempts)
            {
                var backoff = attempt switch
                {
                    1 => TimeSpan.FromSeconds(15),
                    2 => TimeSpan.FromSeconds(30),
                    3 => TimeSpan.FromSeconds(60),
                    4 => TimeSpan.FromSeconds(120),
                    _ => TimeSpan.FromSeconds(240)
                };
                var available = now.Add(backoff);
                store.Requeue(job.Id, attempt, available);
                uow.SaveChanges();
                _logger.Warning("LeaseExpiredRequeue {JobId} {Attempt} {AvailableAt}", job.Id, attempt, available);
            }
            else
            {
                const string msg = "max attempts reached";
                await _fs.SaveTextAtomic(job.Id, "error.txt", msg, ct);
                store.MarkFailed(job.Id, msg);
                uow.SaveChanges();
                _logger.Warning("MaxAttemptsReached {JobId}", job.Id);
            }
        }
    }
}
