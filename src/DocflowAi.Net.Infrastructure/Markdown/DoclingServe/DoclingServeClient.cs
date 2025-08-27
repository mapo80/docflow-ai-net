using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocflowAi.Net.Infrastructure.Markdown.DoclingServe;

public sealed class DoclingServeClient
{
    private readonly HttpClient _http;

    public DoclingServeClient(string baseUrl, HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<ConvertDocumentResponse> ConvertFileAsync(Stream file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file), "files", "file");
        content.Add(new StringContent("inbody"), "target_type");
        using var resp = await _http.PostAsync("/v1/convert/file", content, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<ConvertDocumentResponse>(stream, cancellationToken: ct);
        return result ?? new ConvertDocumentResponse();
    }
}

public sealed class ConvertDocumentResponse
{
    [JsonPropertyName("document")]
    public ExportDocumentResponse? Document { get; set; }
}

public sealed class ExportDocumentResponse
{
    [JsonPropertyName("md_content")]
    public string? MdContent { get; set; }
}

