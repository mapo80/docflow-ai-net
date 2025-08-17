using System.Text.Json.Nodes;

namespace DocflowRules.Api.DTO;

public record RuleSummaryDto(Guid Id, string Name, string Version, bool IsBuiltin, bool Enabled, string? Description, DateTimeOffset UpdatedAt);
public record RuleDetailDto(Guid Id, string Name, string Version, bool IsBuiltin, bool Enabled, string? Description, string Code, string? ReadsCsv, string? WritesCsv, DateTimeOffset UpdatedAt);

public record RunResultDto(JsonObject Before, JsonObject After, JsonArray Mutations, long DurationMs, string[] Logs);
