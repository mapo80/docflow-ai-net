namespace DocflowAi.Net.Api.Contracts;

/// <summary>Uniform error payload returned by the API.</summary>
/// <param name="Error">Short machine readable error code.</param>
/// <param name="Message">Human readable message.</param>
/// <param name="RetryAfterSeconds">Retry hint in seconds when applicable.</param>
public record ErrorResponse(string Error, string? Message, int? RetryAfterSeconds = null);
