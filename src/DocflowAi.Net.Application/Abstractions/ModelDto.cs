namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// DTO representing a model without exposing secrets.
/// </summary>
public record ModelDto(
    Guid Id,
    string Name,
    string Type,
    string? Provider,
    string? HfRepo,
    string? ModelFile,
    bool? Downloaded,
    string? DownloadStatus,
    DateTimeOffset? LastUsedAt,
    bool HasApiKey,
    bool HasHfToken);
