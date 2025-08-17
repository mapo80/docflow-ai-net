namespace DocflowAi.Net.Api.Features.Models.Downloaders;

public class HuggingFaceDownloader : IModelDownloader
{
    private readonly HttpClient _http;

    public HuggingFaceDownloader(HttpClient http) => _http = http;

    public bool CanHandle(GgufModel model) =>
        model.SourceType == ModelSourceType.HuggingFace &&
        !string.IsNullOrWhiteSpace(model.HfRepo) &&
        !string.IsNullOrWhiteSpace(model.HfFilename);

    public async Task DownloadAsync(GgufModel model, string targetPath, IProgress<int> progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.HfRepo) || string.IsNullOrWhiteSpace(model.HfFilename))
            throw new InvalidOperationException("HuggingFace repo/filename not set");

        var rev = string.IsNullOrWhiteSpace(model.HfRevision) ? "main" : model.HfRevision;
        var url = $"https://huggingface.co/{model.HfRepo}/resolve/{rev}/{model.HfFilename}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        // If you use a token, supply it via DefaultRequestHeaders.Authorization before calling.
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
