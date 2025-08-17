
namespace DocflowAi.Net.Api.Features.Models;

public record ModelActivationPayload(
    ModelSourceType SourceType,
    string? LocalPath,
    string? Endpoint,
    string? ApiKey,
    string? Model,
    string? Organization,
    string? ApiVersion,
    string? Deployment,
    string? ExtraHeadersJson
);
