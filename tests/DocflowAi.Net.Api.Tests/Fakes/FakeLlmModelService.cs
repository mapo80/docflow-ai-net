using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Api.Tests.Fakes;

public sealed class FakeLlmModelService : ILlmModelService
{
    private double _pct = 100;
    private bool _completed = true;
    private bool _running;

    public Task SwitchModelAsync(string hfKey, string modelRepo, string modelFile, int contextSize, CancellationToken ct)
    {
        if (_running)
            throw new InvalidOperationException("switch in progress");
        _running = true;
        _completed = false;
        _pct = 0;
        _ = Task.Run(async () =>
        {
            for (var i = 1; i <= 5; i++)
            {
                await Task.Delay(20);
                _pct = i * 20;
            }
            _completed = true;
            _running = false;
        });
        return Task.CompletedTask;
    }

    public ModelDownloadStatus GetStatus() => new(_completed, _pct);
}
