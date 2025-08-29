using Polly;
using Polly.Retry;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Application.Abstractions;
using Serilog;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Resolves the model definition based on the provided token and invokes the
/// corresponding backend: local models, OpenAI compatible APIs or Azure OpenAI.
/// </summary>
public class ModelDispatchService : IModelDispatchService
{
    private readonly IModelRepository _repo;
    private readonly ISecretProtector _protector;
    private readonly Serilog.ILogger _logger = Log.ForContext<ModelDispatchService>();
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly IReadOnlyDictionary<string, IHostedModelProvider> _providers;

    public ModelDispatchService(IModelRepository repo, ISecretProtector protector, IEnumerable<IHostedModelProvider> providers)
    {
        _repo = repo;
        _protector = protector;
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _retryPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.Warning(ex, "Retrying model call in {Delay}ms (attempt {Attempt})", delay.TotalMilliseconds, attempt));
    }

    public async Task<string> InvokeAsync(string modelToken, string payload, CancellationToken ct)
    {
        var model = _repo.GetByName(modelToken) ??
            throw new InvalidOperationException($"Model '{modelToken}' not found");

        return model.Type switch
        {
            "local" => HandleLocal(model, payload),
            "hosted-llm" => await HandleHostedAsync(model, payload, ct),
            _ => throw new NotSupportedException($"Unknown model type '{model.Type}'")
        };
    }

    private string HandleLocal(ModelDocument model, string payload)
    {
        _logger.Information("LocalModelInvoked {Name}", model.Name);
        return payload; // local execution pipeline will be implemented separately
    }

    private async Task<string> HandleHostedAsync(ModelDocument model, string payload, CancellationToken ct)
    {
        if (!_providers.TryGetValue(model.Provider, out var provider))
            throw new NotSupportedException($"Unknown hosted provider '{model.Provider}'");

        var baseUrl = model.BaseUrl ?? throw new InvalidOperationException("BaseUrl is required for hosted models");
        var apiKey = string.IsNullOrEmpty(model.ApiKeyEncrypted)
            ? null
            : _protector.Unprotect(model.ApiKeyEncrypted);

        return await _retryPolicy.ExecuteAsync(innerCt =>
            provider.InvokeAsync(model.Name, baseUrl, apiKey, payload, innerCt), ct);
    }
}
