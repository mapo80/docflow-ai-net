namespace DocflowAi.Net.Api.Features.Models.Downloaders;

public interface IModelDownloader
{
    bool CanHandle(GgufModel model);
    Task DownloadAsync(GgufModel model, string targetPath, IProgress<int> progress, CancellationToken ct);
}
