namespace DocflowAi.Net.Application.Abstractions;

public interface ILlmModelService
{
    Task SwitchModelAsync(string hfKey, string modelRepo, string modelFile, int contextSize, CancellationToken ct);
}
