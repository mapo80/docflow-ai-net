namespace DocflowAi.Net.Application.Abstractions;

public sealed record ModelInfo(
    string? Name,
    string? Repo,
    string? File,
    int? ContextSize,
    DateTime? LoadedAt
);
