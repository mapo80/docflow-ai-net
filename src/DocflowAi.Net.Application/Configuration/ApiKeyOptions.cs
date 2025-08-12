namespace DocflowAi.Net.Application.Configuration;
public sealed class ApiKeyOptions { public const string SectionName = "Api"; public string HeaderName { get; set; } = "X-API-Key"; public string[] Keys { get; set; } = []; }
