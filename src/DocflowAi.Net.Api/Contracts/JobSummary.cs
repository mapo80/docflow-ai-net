using DocflowAi.Net.Api.JobQueue.Models;

namespace DocflowAi.Net.Api.Contracts;

/// <summary>Summary information for a job.</summary>
public record JobSummary(Guid Id, string Status, string DerivedStatus, int Progress, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Paged list of jobs.</summary>
public record PagedJobsResponse(int Page, int PageSize, int Total, IReadOnlyList<JobSummary> Items);

/// <summary>Response returned when a job is queued.</summary>
public record SubmitAcceptedResponse(Guid job_id, string status_url, string? dashboard_url);

/// <summary>Response returned when a job completes immediately.</summary>
public record ImmediateJobResponse(Guid job_id, string status, long? duration_ms = null, string? error = null);

/// <summary>Detailed job information.</summary>
public record JobDetailResponse(Guid Id, string Status, string DerivedStatus, int Progress, int Attempts, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, JobDocument.MetricsInfo Metrics, JobDocument.PathInfo Paths, string? ErrorMessage);
