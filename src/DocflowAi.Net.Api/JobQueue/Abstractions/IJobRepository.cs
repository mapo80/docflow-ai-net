using DocflowAi.Net.Api.JobQueue.Models;
using System;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IJobRepository
{
    JobDocument? Get(Guid id);
    (IReadOnlyList<JobDocument> items, int total) ListPaged(int page, int pageSize);
    (IReadOnlyList<JobDocument> items, int total) ListPagedFiltered(int page, int pageSize, string? q, string[]? statuses, DateTimeOffset? from, DateTimeOffset? to, bool? immediate);
    void Create(JobDocument doc);
    void UpdateStatus(Guid id, string status, string? errorMessage = null, DateTimeOffset? endedAt = null, long? durationMs = null);
    void UpdateProgress(Guid id, int progress);
    void TouchLease(Guid id, DateTimeOffset leaseUntil);
    int CountPending();
    void MarkFailed(Guid id, string errorMessage);
    void MarkSucceeded(Guid id, DateTimeOffset endedAt, long durationMs);
    void MarkCancelled(Guid id, string? reason);
    JobDocument? FindByIdempotencyKey(string key, TimeSpan ttl);
    JobDocument? FindRecentByHash(string hash, TimeSpan ttl);
    IEnumerable<JobDocument> FindQueuedDue(DateTimeOffset now);
    IEnumerable<JobDocument> FindRunningExpired(DateTimeOffset now);
    void Requeue(Guid id, int attempts, DateTimeOffset availableAt);
    IEnumerable<JobDocument> DeleteOlderThan(DateTimeOffset cutoff);
}
