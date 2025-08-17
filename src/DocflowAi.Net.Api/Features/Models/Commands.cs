using System.ComponentModel.DataAnnotations;

namespace DocflowAi.Net.Api.Features.Models;

public record AddModelRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required] ModelSourceType SourceType,
    [property: MaxLength(2048)] string? LocalPath,
    [property: MaxLength(2048)] string? Url,
    [property: MaxLength(256)] string? HfRepo,
    [property: MaxLength(256)] string? HfRevision,
    [property: MaxLength(256)] string? HfFilename,
    [property: MaxLength(128)] string? Sha256,
    [property: MaxLength(2048)] string? Endpoint,
    [property: MaxLength(512)] string? ApiKey,
    [property: MaxLength(256)] string? Model,
    [property: MaxLength(256)] string? Organization,
    [property: MaxLength(64)] string? ApiVersion,
    [property: MaxLength(256)] string? Deployment,
    string? ExtraHeadersJson
);

public record ModelDto(
    Guid Id,
    string Name,
    ModelSourceType SourceType,
    string? LocalPath,
    string? Url,
    string? HfRepo,
    string? HfRevision,
    string? HfFilename,
    string? Sha256,
    long? FileSize,
    ModelStatus Status,
    int DownloadProgress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    string? ErrorMessage
)
{
    public static ModelDto FromEntity(GgufModel e) => new(
        e.Id, e.Name, e.SourceType, e.LocalPath, e.Url, e.HfRepo, e.HfRevision, e.HfFilename,
        e.Sha256, e.FileSize, e.Status, e.DownloadProgress, e.CreatedAt, e.LastUsedAt, e.IsActive, e.ErrorMessage
    );
}
