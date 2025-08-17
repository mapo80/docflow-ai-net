using System.Net.Http.Headers;

namespace DocflowAi.Net.Api.Features.Models.Downloaders;

public class HttpModelDownloader : IModelDownloader
{
    private readonly HttpClient _http;

    public HttpModelDownloader(HttpClient http) => _http = http;

    public bool CanHandle(GgufModel model) => model.SourceType == ModelSourceType.Url && !string.IsNullOrWhiteSpace(model.Url);

    public async Task DownloadAsync(GgufModel model, string targetPath, IProgress<int> progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Url)) throw new InvalidOperationException("URL not set");

        using var req = new HttpRequestMessage(HttpMethod.Get, model.Url);
        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        var total = res.Content.Headers.ContentLength;
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        await using var file = File.Create(targetPath);

        var buffer = new byte[81920];
        long readSoFar = 0;
        int read;
        int lastPct = 0;

        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), ct);
            readSoFar += read;
            if (total.HasValue && total.Value > 0)
            {
                var pct = (int)(readSoFar * 100 / total.Value);
                if (pct != lastPct)
                {
                    lastPct = pct;
                    progress.Report(pct);
                }
            }
        }
        progress.Report(100);
    }
}
