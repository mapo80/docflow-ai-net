using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.JobQueue.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DocflowAi.Net.Api.JobQueue.Repositories;

public class JobRepository : IJobRepository
{
    private readonly JobDbContext _db;
    private readonly ILogger<JobRepository> _logger;

    public JobRepository(JobDbContext db, ILogger<JobRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public JobDocument? Get(Guid id) => _db.Jobs.Find(id);

    public (IReadOnlyList<JobDocument> items, int total) ListPaged(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;
        if (pageSize <= 0) pageSize = 20;
        var query = _db.Jobs.OrderByDescending(x => x.CreatedAt);
        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public (IReadOnlyList<JobDocument> items, int total) ListPagedFiltered(int page, int pageSize, string? q, string[]? statuses, DateTimeOffset? from, DateTimeOffset? to, bool? immediate)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;
        if (pageSize <= 0) pageSize = 20;
        var query = _db.Jobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.ToLower();
            query = query.Where(x =>
                x.Id.ToString().ToLower().Contains(s) ||
                x.Paths.Input.ToLower().Contains(s) ||
                (x.Paths.Prompt != null && x.Paths.Prompt.ToLower().Contains(s)) ||
                (x.ErrorMessage != null && x.ErrorMessage.ToLower().Contains(s))
            );
        }

        if (statuses != null && statuses.Length > 0)
        {
            query = query.Where(x => statuses.Contains(x.Status));
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= to.Value);
        }

        if (immediate.HasValue)
        {
            query = query.Where(x => x.Immediate == immediate.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);
        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public void Create(JobDocument doc)
    {
        doc.CreatedAt = doc.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Jobs.Add(doc);
        _logger.LogInformation("JobCreated {JobId} {Status}", doc.Id, doc.Status);
    }

    public void UpdateStatus(Guid id, string status, string? errorMessage = null, DateTimeOffset? endedAt = null, long? durationMs = null)
    {
        var doc = _db.Jobs.Find(id);
        if (doc == null) return;
        doc.Status = status;
        doc.ErrorMessage = errorMessage;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        if (endedAt.HasValue) doc.Metrics.EndedAt = endedAt;
        if (durationMs.HasValue) doc.Metrics.DurationMs = durationMs;
        _logger.LogInformation("UpdateStatus {JobId} {Status}", id, status);
    }

    public void UpdateProgress(Guid id, int progress)
    {
        var doc = _db.Jobs.Find(id);
        if (doc == null) return;
        doc.Progress = progress;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogDebug("UpdateProgress {JobId} {Progress}", id, progress);
    }

    public void TouchLease(Guid id, DateTimeOffset leaseUntil)
    {
        var doc = _db.Jobs.Find(id);
        if (doc == null) return;
        doc.LeaseUntil = leaseUntil;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogDebug("TouchLease {JobId} {LeaseUntil}", id, leaseUntil);
    }

    public int CountPending()
    {
        var count = _db.Jobs.Count(x => x.Status == "Queued" || x.Status == "Running");
        _logger.LogDebug("CountPending {QueuedCount}", count);
        return count;
    }

    public void MarkFailed(Guid id, string errorMessage)
    {
        UpdateStatus(id, "Failed", errorMessage, DateTimeOffset.UtcNow, null);
        _logger.LogWarning("MarkFailed {JobId} {Error}", id, errorMessage);
    }

    public void MarkSucceeded(Guid id, DateTimeOffset endedAt, long durationMs)
    {
        UpdateStatus(id, "Succeeded", null, endedAt, durationMs);
        _logger.LogInformation("MarkSucceeded {JobId}", id);
    }

    public void MarkCancelled(Guid id, string? reason)
    {
        UpdateStatus(id, "Cancelled", reason);
        _logger.LogWarning("MarkCancelled {JobId} {Reason}", id, reason);
    }

    public JobDocument? FindByIdempotencyKey(string key, TimeSpan ttl)
    {
        var threshold = DateTimeOffset.UtcNow - ttl;
        var job = _db.Jobs
            .FirstOrDefault(x => x.IdempotencyKey == key && x.CreatedAt >= threshold);
        _logger.LogDebug("FindByIdempotencyKey {Key} {Hit}", key, job != null);
        return job;
    }

    public JobDocument? FindRecentByHash(string hash, TimeSpan ttl)
    {
        var threshold = DateTimeOffset.UtcNow - ttl;
        var job = _db.Jobs
            .FirstOrDefault(x => x.Hash == hash && x.CreatedAt >= threshold && x.Status != "Cancelled");
        _logger.LogDebug("FindRecentByHash {Hash} {Hit}", hash, job != null);
        return job;
    }

    public IEnumerable<JobDocument> FindQueuedDue(DateTimeOffset now)
    {
        var jobs = _db.Jobs
            .Where(x => x.Status == "Queued" && (!x.AvailableAt.HasValue || x.AvailableAt <= now))
            .ToList();
        _logger.LogDebug("FindQueuedDue {Count}", jobs.Count);
        return jobs;
    }

    public IEnumerable<JobDocument> FindRunningExpired(DateTimeOffset now)
    {
        var jobs = _db.Jobs
            .Where(x => x.Status == "Running" && x.LeaseUntil.HasValue && x.LeaseUntil <= now)
            .ToList();
        _logger.LogDebug("FindRunningExpired {Count}", jobs.Count);
        return jobs;
    }

    public void Requeue(Guid id, int attempts, DateTimeOffset availableAt)
    {
        var doc = _db.Jobs.Find(id);
        if (doc == null) return;
        doc.Status = "Queued";
        doc.Attempts = attempts;
        doc.AvailableAt = availableAt;
        doc.LeaseUntil = null;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogWarning("Requeue {JobId} {Attempt} {AvailableAt}", id, attempts, availableAt);
    }

    public IEnumerable<JobDocument> DeleteOlderThan(DateTimeOffset cutoff)
    {
        var old = _db.Jobs
            .Where(x => x.CreatedAt < cutoff)
            .ToList();
        _db.Jobs.RemoveRange(old);
        _logger.LogInformation("DeleteOlderThan {Cutoff} {Count}", cutoff, old.Count);
        return old;
    }
}
