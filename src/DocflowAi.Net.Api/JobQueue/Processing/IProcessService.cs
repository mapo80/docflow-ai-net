namespace DocflowAi.Net.Api.JobQueue.Processing;

public record ProcessInput(Guid JobId, string InputPath, string? PromptPath, string? FieldsPath);
public record ProcessResult(bool Success, string OutputJson, string? ErrorMessage);

public interface IProcessService
{
    /// <summary>
    /// Executes the internal "process" pipeline for the given job artifacts.
    /// The service must not mutate job state or write to the filesystem.
    /// </summary>
    /// <param name="input">Paths to job artifacts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the processing.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct);
}
