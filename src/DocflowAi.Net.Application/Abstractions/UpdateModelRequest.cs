using System;

namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Request payload for updating a hosted model.
/// </summary>
public class UpdateModelRequest
{
    public required string Name { get; init; }
    public required string Provider { get; init; }
    public required string BaseUrl { get; init; }
    public string? ApiKey { get; init; }
}
