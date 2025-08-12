using DocflowAi.Net.Application.Abstractions; using DocflowAi.Net.Application.Configuration; using Microsoft.Extensions.Logging; using Microsoft.Extensions.Options; using System.Net.Http.Headers;
namespace DocflowAi.Net.Infrastructure.Markitdown;
public sealed class MarkitdownClient : IMarkitdownClient {
    private readonly HttpClient _http; private readonly ILogger<MarkitdownClient> _logger;
    public MarkitdownClient(HttpClient http, IOptions<ServicesOptions> options, ILogger<MarkitdownClient> logger) {
        _http = http; _logger = logger; _http.BaseAddress = new Uri(options.Value.Markitdown.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(options.Value.Markitdown.TimeoutSeconds);
    }
    public async Task<string> ToMarkdownAsync(Stream imageStream, string fileName, CancellationToken ct) {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(imageStream) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }}, "file", fileName);
        _logger.LogInformation("Calling Markitdown service at {BaseUrl}", _http.BaseAddress);
        using var resp = await _http.PostAsync("/markdown", content, ct); resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadAsStringAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(payload);
        var md = doc.RootElement.GetProperty("markdown").GetString() ?? string.Empty;
        _logger.LogInformation("Received markdown with {Length} chars", md.Length); return md;
    }
}
