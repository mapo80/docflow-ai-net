using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.JobQueue.Models;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.JobQueue.Services;

public class LiteDbJobStore : IJobStore
{
    private readonly ILiteCollection<JobDocument> _collection;
    private readonly ILogger<LiteDbJobStore> _logger;

    public LiteDbJobStore(LiteDatabase db, ILogger<LiteDbJobStore> logger)
    {
        _logger = logger;
        _collection = db.GetCollection<JobDocument>("jobs");
        _collection.EnsureIndex(x => x.CreatedAt);
        _collection.EnsureIndex("status_available", x => new { x.Status, x.AvailableAt });
        _collection.EnsureIndex(x => x.IdempotencyKey);
        _collection.EnsureIndex(x => x.Hash);
    }

    public JobDocument? Get(Guid id) => _collection.FindById(id);

    public (IReadOnlyList<JobDocument> items, int total) ListPaged(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;
        if (pageSize <= 0) pageSize = 20;
        var query = _collection.Query().OrderByDescending(x => x.CreatedAt);
        var total = _collection.Count();
        var items = query.Skip((page - 1) * pageSize).Limit(pageSize).ToList();
        return (items, total);
    }

    public void Create(JobDocument doc)
    {
        doc.CreatedAt = doc.UpdatedAt = DateTimeOffset.UtcNow;
        _collection.Insert(doc);
        _logger.LogInformation("JobCreated {JobId} {Status}", doc.Id, doc.Status);
    }

    public void UpdateStatus(Guid id, string status, string? errorMessage = null, DateTimeOffset? endedAt = null, long? durationMs = null)
    {
        var doc = _collection.FindById(id);
        if (doc == null) return;
        doc.Status = status;
        doc.ErrorMessage = errorMessage;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        if (endedAt.HasValue) doc.Metrics.EndedAt = endedAt;
        if (durationMs.HasValue) doc.Metrics.DurationMs = durationMs;
        _collection.Update(doc);
        _logger.LogInformation("UpdateStatus {JobId} {Status}", id, status);
    }

    public void UpdateProgress(Guid id, int progress)
    {
        var doc = _collection.FindById(id);
        if (doc == null) return;
        doc.Progress = progress;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _collection.Update(doc);
        _logger.LogDebug("UpdateProgress {JobId} {Progress}", id, progress);
    }

    public void TouchLease(Guid id, DateTimeOffset leaseUntil)
    {
        var doc = _collection.FindById(id);
        if (doc == null) return;
        doc.LeaseUntil = leaseUntil;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _collection.Update(doc);
        _logger.LogDebug("TouchLease {JobId} {LeaseUntil}", id, leaseUntil);
    }

    public int CountPending()
    {
        var count = _collection.Count(x => x.Status == "Queued" || x.Status == "Running");
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
        var job = _collection.FindOne(x => x.IdempotencyKey == key && x.CreatedAt >= threshold);
        _logger.LogDebug("FindByIdempotencyKey {Key} {Hit}", key, job != null);
        return job;
    }

    public JobDocument? FindRecentByHash(string hash, TimeSpan ttl)
    {
        var threshold = DateTimeOffset.UtcNow - ttl;
        var job = _collection.FindOne(x => x.Hash == hash && x.CreatedAt >= threshold && x.Status != "Cancelled");
        _logger.LogDebug("FindRecentByHash {Hash} {Hit}", hash, job != null);
        return job;
    }
    public IEnumerable<JobDocument> FindQueuedDue(DateTimeOffset now)
    {
        var jobs = _collection.Find(x => x.Status == "Queued" && x.AvailableAt <= now).ToList();
        _logger.LogDebug("FindQueuedDue {Count}", jobs.Count);
        return jobs;
    }

    public IEnumerable<JobDocument> FindRunningExpired(DateTimeOffset now)
    {
        var jobs = _collection.Find(x => x.Status == "Running" && x.LeaseUntil != null && x.LeaseUntil < now).ToList();
        _logger.LogDebug("FindRunningExpired {Count}", jobs.Count);
        return jobs;
    }

    public void Requeue(Guid id, int attempts, DateTimeOffset availableAt)
    {
        var doc = _collection.FindById(id);
        if (doc == null) return;
        doc.Status = "Queued";
        doc.Attempts = attempts;
        doc.AvailableAt = availableAt;
        doc.LeaseUntil = null;
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _collection.Update(doc);
        _logger.LogWarning("Requeue {JobId} {Attempt} {AvailableAt}", id, attempts, availableAt);
    }

    public IEnumerable<JobDocument> DeleteOlderThan(DateTimeOffset cutoff)
    {
        var old = _collection.Find(x => x.CreatedAt < cutoff).ToList();
        foreach (var doc in old)
            _collection.Delete(doc.Id);
        _logger.LogInformation("DeleteOlderThan {Cutoff} {Count}", cutoff, old.Count);
        return old;
    }
}

