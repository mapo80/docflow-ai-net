using DocflowAi.Net.Api.JobQueue.Models;
using System;
using System.Collections.Generic;

namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IJobRepository
{
    JobDocument? Get(Guid id);
    (IReadOnlyList<JobDocument> items, int total) ListPaged(int page, int pageSize);
    void Create(JobDocument doc);
    void UpdateStatus(Guid id, string status, string? errorMessage = null, DateTimeOffset? endedAt = null, long? durationMs = null);
    void UpdateProgress(Guid id, int progress);
    void IncrementAttempts(Guid id);
    int CountPending();
    void MarkFailed(Guid id, string errorMessage);
    void MarkSucceeded(Guid id, DateTimeOffset endedAt, long durationMs);
    void MarkCancelled(Guid id, string? reason);
    JobDocument? FindByIdempotencyKey(string key, TimeSpan ttl);
    JobDocument? FindRecentByHash(string hash, TimeSpan ttl);
    IEnumerable<JobDocument> DeleteOlderThan(DateTimeOffset cutoff);
}
