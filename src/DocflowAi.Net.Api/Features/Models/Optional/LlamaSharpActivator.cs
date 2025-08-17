// This sample shows how to wire LLamaSharp as an IModelActivator.
// Guard with a conditional symbol or add the proper package references in your csproj.

#if LLAMASHARP
using DocflowAi.Net.Api.Features.Models;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;

public class LlamaSharpModelActivator : IModelActivator, IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _ctx;
    private readonly ILogger<LlamaSharpModelActivator> _log;

    public LlamaSharpModelActivator(ILogger<LlamaSharpModelActivator> log) => _log = log;

    public async Task ActivateAsync(string localPath, CancellationToken ct = default)
    {
        DisposeCurrent();
        _log.LogInformation("Loading LLamaSharp model: {Path}", localPath);
        _weights = await LLamaWeights.LoadFromFileAsync(localPath, ct: ct);
        _ctx = _weights.CreateContext(new LLamaContextParams());
    }

    private void DisposeCurrent()
    {
        _ctx?.Dispose();
        _ctx = null;
        _weights?.Dispose();
        _weights = null;
    }

    public void Dispose() => DisposeCurrent();
}
#endif
