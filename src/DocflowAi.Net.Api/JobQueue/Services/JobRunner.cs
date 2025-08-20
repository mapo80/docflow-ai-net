using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Options;
using System.Threading;
using Microsoft.Extensions.Options;
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

    public async Task Run(Guid jobId, Hangfire.IJobCancellationToken? jobToken, CancellationToken ct, bool acquireGate = true, int? overrideTimeoutSeconds = null)
    {
        using (LogContext.PushProperty("JobId", jobId))
        {
            var sw = Stopwatch.StartNew();
            if (acquireGate)
                await _gate.WaitAsync(ct);
            jobToken ??= new Hangfire.JobCancellationToken(false);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, jobToken.ShutdownToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(overrideTimeoutSeconds ?? _options.Timeouts.JobTimeoutSeconds));
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
                _store.IncrementAttempts(jobId);
                job.Metrics.StartedAt = DateTimeOffset.UtcNow;
                _uow.SaveChanges();
                _logger.Information("JobStarted {JobId}", jobId);

                ProcessResult result;
                try
                {
                    var input = new ProcessInput(jobId, job.Paths.Input!.Path, job.Paths.Markdown!.Path, job.Paths.Prompt!.Path, job.TemplateToken, job.Model);
                    result = await _process.ExecuteAsync(input, linkedCts.Token);
                    if (result.MarkdownCreatedAt.HasValue)
                        job.Paths.Markdown!.CreatedAt = result.MarkdownCreatedAt;
                    if (result.PromptCreatedAt.HasValue)
                        job.Paths.Prompt!.CreatedAt = result.PromptCreatedAt;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested || jobToken.ShutdownToken.IsCancellationRequested)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error!.Path), "cancelled by user");
                    job.Paths.Error!.CreatedAt = DateTimeOffset.UtcNow;
                    _store.MarkCancelled(jobId, "cancelled by user");
                    _uow.SaveChanges();
                    _logger.Warning("JobCancelled {JobId}", jobId);
                    jobToken.ThrowIfCancellationRequested();
                    return;
                }
                catch (OperationCanceledException)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error!.Path), "timeout");
                    job.Paths.Error!.CreatedAt = DateTimeOffset.UtcNow;
                    _store.MarkFailed(jobId, "timeout");
                    _uow.SaveChanges();
                    _logger.Warning("JobTimeout {JobId}", jobId);
                    throw;
                }
                catch (Exception ex)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error!.Path), ex.Message);
                    job.Paths.Error!.CreatedAt = DateTimeOffset.UtcNow;
                    _store.MarkFailed(jobId, ex.Message);
                    _uow.SaveChanges();
                    _logger.Error(ex, "JobFailed {JobId}", jobId);
                    throw;
                }

                if (result.Success)
                {
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Output!.Path!), result.OutputJson);
                    job.Paths.Output!.CreatedAt = DateTimeOffset.UtcNow;
                    var ended = DateTimeOffset.UtcNow;
                    _store.MarkSucceeded(jobId, ended, (long)(ended - job.Metrics.StartedAt!.Value).TotalMilliseconds);
                    _store.UpdateProgress(jobId, 100);
                    _uow.SaveChanges();
                    _logger.Information("JobCompleted {JobId}", jobId);
                }
                else
                {
                    var msg = result.ErrorMessage ?? "unknown error";
                    await _fs.SaveTextAtomic(jobId, Path.GetFileName(job.Paths.Error!.Path), msg);
                    job.Paths.Error!.CreatedAt = DateTimeOffset.UtcNow;
                    _store.MarkFailed(jobId, msg);
                    _uow.SaveChanges();
                    _logger.Warning("JobFailed {JobId} {Error}", jobId, msg);
                    throw new Exception(msg);
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
