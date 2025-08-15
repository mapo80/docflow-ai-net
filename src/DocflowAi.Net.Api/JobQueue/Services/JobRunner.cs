using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Options;
using System.Threading;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace DocflowAi.Net.Api.JobQueue.Services;

public class JobRunner : IJobRunner
{
    private readonly IProcessService _process;
    private readonly IFileSystemService _fs;
    private readonly IJobRepository _store;
    private readonly IUnitOfWork _uow;
    private readonly JobQueueOptions _options;
    private readonly Serilog.ILogger _logger;
    private readonly IConcurrencyGate _gate;

    public JobRunner(IProcessService process, IFileSystemService fs, IJobRepository store, IUnitOfWork uow, IOptions<JobQueueOptions> options, IConcurrencyGate gate)
    {
        _process = process;
        _fs = fs;
        _store = store;
        _uow = uow;
        _options = options.Value;
        _logger = Log.ForContext<JobRunner>();
        _gate = gate;
    }

    public async Task Run(Guid jobId, CancellationToken ct, bool acquireGate = true, int? overrideTimeoutSeconds = null)
    {
        using (LogContext.PushProperty("JobId", jobId))
        {
            var sw = Stopwatch.StartNew();
            if (acquireGate)
                await _gate.WaitAsync(ct);
            try
            {
                var job = _store.Get(jobId);
                if (job == null)
                {
                    _logger.Warning("JobMissing {JobId}", jobId);
                    return;
                }

                _store.UpdateStatus(jobId, "Running");
                _store.UpdateProgress(jobId, 0);
                _uow.SaveChanges();
                var started = DateTimeOffset.UtcNow;
                _store.TouchLease(jobId, started.AddSeconds(_options.Queue.LeaseWindowSeconds));
                _uow.SaveChanges();
                _logger.Information("JobStarted {JobId}", jobId);

                var leaseCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var heartbeat = Task.Run(async () =>
                {
                    while (!leaseCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), leaseCts.Token);
                            _store.TouchLease(jobId, DateTimeOffset.UtcNow.AddSeconds(_options.Queue.LeaseWindowSeconds));
                            _uow.SaveChanges();
                        }
                        catch (OperationCanceledException) { }
                    }
                }, leaseCts.Token);

                var timeoutPolicy = Policy.TimeoutAsync<ProcessResult>(
                    TimeSpan.FromSeconds(overrideTimeoutSeconds ?? _options.Timeouts.JobTimeoutSeconds),
                    TimeoutStrategy.Optimistic);

                ProcessResult result;
                try
                {
                    var input = new ProcessInput(jobId, job.Paths.Input, job.Paths.Prompt, job.Paths.Fields);
                    result = await timeoutPolicy.ExecuteAsync((token) => _process.ExecuteAsync(input, token), leaseCts.Token);
                }
                catch (OperationCanceledException)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error), "cancelled by user");
                    _store.MarkCancelled(jobId, "cancelled by user");
                    _uow.SaveChanges();
                    _logger.Warning("JobCancelled {JobId}", jobId);
                    return;
                }
                catch (TimeoutRejectedException)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error), "timeout");
                    _store.MarkFailed(jobId, "timeout");
                    _uow.SaveChanges();
                    _logger.Warning("JobTimeout {JobId}", jobId);
                    return;
                }
                catch (Exception ex)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error), ex.Message);
                    _store.MarkFailed(jobId, ex.Message);
                    _uow.SaveChanges();
                    _logger.Error(ex, "JobFailed {JobId}", jobId);
                    return;
                }
                finally
                {
                    leaseCts.Cancel();
                    try { await heartbeat; } catch { }
                }

                if (result.Success)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Output), result.OutputJson);
                    var ended = DateTimeOffset.UtcNow;
                    _store.MarkSucceeded(jobId, ended, (long)(ended - started).TotalMilliseconds);
                    _store.UpdateProgress(jobId, 100);
                    _uow.SaveChanges();
                    _logger.Information("JobCompleted {JobId}", jobId);
                }
                else
                {
                    var msg = result.ErrorMessage ?? "unknown error";
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error), msg);
                    _store.MarkFailed(jobId, msg);
                    _uow.SaveChanges();
                    _logger.Warning("JobFailed {JobId} {Error}", jobId, msg);
                }
            }
            finally
            {
                if (acquireGate)
                    _gate.Release();
                _logger.Information("JobFinalized {JobId} {ElapsedMs}", jobId, sw.ElapsedMilliseconds);
            }
        }
    }
}
