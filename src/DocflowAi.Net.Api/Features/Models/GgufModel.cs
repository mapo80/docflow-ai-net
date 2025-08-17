using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocflowAi.Net.Api.Features.Models;

public class GgufModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ModelSourceType SourceType { get; set; }

    // Local path (if already present or post-download destination)
    [MaxLength(2048)]
    public string? LocalPath { get; set; }

    // Remote URL (direct HTTP)
    [MaxLength(2048)]
    public string? Url { get; set; }

    // HuggingFace fields
    [MaxLength(256)]
    public string? HfRepo { get; set; }
    [MaxLength(256)]
    public string? HfRevision { get; set; }
    [MaxLength(256)]
    public string? HfFilename { get; set; }

    [MaxLength(128)]
    public string? Sha256 { get; set; }

    public long? FileSize { get; set; }

    public ModelStatus Status { get; set; } = ModelStatus.NotDownloaded;
    // Generic provider settings (used for OpenAI / Azure OpenAI / OpenAI-compatible)
    [MaxLength(2048)]
    public string? Endpoint { get; set; }   // e.g., https://api.openai.com/v1 or Azure endpoint

    [MaxLength(512)]
    public string? ApiKey { get; set; }

    [MaxLength(256)]
    public string? Model { get; set; }      // e.g., gpt-4o-mini, deployment name for Azure if you prefer

    [MaxLength(256)]
    public string? Organization { get; set; } // OpenAI org (optional)

    [MaxLength(64)]
    public string? ApiVersion { get; set; } // Azure OpenAI api-version (optional)

    [MaxLength(256)]
    public string? Deployment { get; set; } // Azure OpenAI deployment (optional)

    public string? ExtraHeadersJson { get; set; } // JSON string for extra headers if needed


    [Range(0,100)]
    public int DownloadProgress { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = false;

    [MaxLength(2048)]
    public string? ErrorMessage { get; set; }
}
