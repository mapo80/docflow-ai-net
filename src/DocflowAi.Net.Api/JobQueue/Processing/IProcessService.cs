using System;

namespace DocflowAi.Net.Api.JobQueue.Processing;

public record ProcessInput(Guid JobId, string InputPath, string MarkdownPath, string PromptPath, string TemplateToken, string Model);
public record ProcessResult(bool Success, string OutputJson, string? Markdown, string? ErrorMessage, DateTimeOffset? MarkdownCreatedAt, DateTimeOffset? PromptCreatedAt);

public interface IProcessService
{
    /// <summary>
    /// Executes the internal "process" pipeline for the given job artifacts.
    /// The service may persist intermediate files but must not mutate job state.
    /// </summary>
    /// <param name="input">Paths to job artifacts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the processing.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct);
}
