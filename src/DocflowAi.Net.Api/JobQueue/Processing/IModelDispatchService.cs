namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Executes a text generation request against the model identified by its token.
/// Selects the proper backend depending on model configuration.
/// </summary>
public interface IModelDispatchService
{
    /// <summary>
    /// Dispatches the request to the model associated with <paramref name="modelToken"/>.
    /// </summary>
    /// <param name="modelToken">Identifier of the model to use.</param>
    /// <param name="payload">JSON payload to send to the model backend.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw JSON returned by the model backend.</returns>
    Task<string> InvokeAsync(string modelToken, string payload, CancellationToken ct);
}
