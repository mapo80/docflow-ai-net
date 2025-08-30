using DocflowAi.Net.Application.Abstractions;
using System.Collections.Generic;
using System;

namespace DocflowAi.Net.Api.Tests.Fakes;

public sealed class ConfigurableFakeLlmModelService : ILlmModelService
{
    private double _pct = 100;
    private bool _completed = true;
    private bool _running;
    private Exception? _next;
    private IEnumerable<string> _models = new List<string> { "f" };
    private ModelInfo _current = new(null, null, null, null, null);

    public void FailWith(Exception ex) => _next = ex;
    public void SetModels(IEnumerable<string> models) => _models = models;
    public void SetCurrent(ModelInfo model) => _current = model;

    public Task DownloadModelAsync(string hfKey, string modelRepo, string modelFile, CancellationToken ct)
    {
        if (_running)
            throw new InvalidOperationException("download in progress");
        if (_next != null)
        {
            var ex = _next;
            _next = null;
            throw ex;
        }
        _running = true;
        _completed = false;
        _pct = 0;
        _ = Task.Run(async () =>
        {
            try
            {
                for (var i = 1; i <= 5; i++)
                {
                    await Task.Delay(20, ct);
                    _pct = i * 20;
                }
                _completed = true;
            }
            catch (OperationCanceledException)
            {
                // ignore cancellation
            }
            finally
            {
                _running = false;
                if (!_completed)
                {
                    _completed = true;
                }
            }
        });
        return Task.CompletedTask;
    }

    public Task SwitchModelAsync(string modelFile, int contextSize)
    {
        if (_next != null)
        {
            var ex = _next;
            _next = null;
            throw ex;
        }
        _current = new ModelInfo(null, null, modelFile, contextSize, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public IEnumerable<string> ListAvailableModels() => _models;

    public ModelDownloadStatus GetStatus() => new(_completed, _pct);

    public ModelInfo GetCurrentModel() => _current;
}
