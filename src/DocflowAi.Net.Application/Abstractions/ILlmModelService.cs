using System.Collections.Generic;

namespace DocflowAi.Net.Application.Abstractions;

public interface ILlmModelService
{
    Task DownloadModelAsync(string hfKey, string modelRepo, string modelFile, CancellationToken ct);

    Task SwitchModelAsync(string modelFile, int contextSize);

    IEnumerable<string> ListAvailableModels();

    ModelDownloadStatus GetStatus();

    ModelInfo GetCurrentModel();
}
