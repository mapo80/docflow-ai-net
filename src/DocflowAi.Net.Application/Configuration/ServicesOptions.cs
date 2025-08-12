namespace DocflowAi.Net.Application.Configuration;
public sealed class ServicesOptions { public const string SectionName = "Services"; public required MarkitdownOptions Markitdown { get; set; } }
public sealed class MarkitdownOptions { public required string BaseUrl { get; set; } public int TimeoutSeconds { get; set; } = 60; public int RetryCount { get; set; } = 3; }
