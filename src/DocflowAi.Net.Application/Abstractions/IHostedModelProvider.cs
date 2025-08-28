namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Dispatches hosted LLM calls for a specific provider.</summary>
public interface IHostedModelProvider
{
    /// <summary>Unique provider identifier.</summary>
    string Name { get; }

    /// <summary>
    /// Invoke the hosted model with the given parameters and return the raw response.
    /// </summary>
    Task<string> InvokeAsync(string model, string endpoint, string? apiKey, string payload, CancellationToken ct);
}
