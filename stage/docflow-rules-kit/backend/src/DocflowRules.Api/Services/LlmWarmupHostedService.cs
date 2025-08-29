using DocflowRules.Storage.EF;

namespace DocflowRules.Api.Services;

public class LlmWarmupHostedService : IHostedService
{
    private readonly ILlmConfigService _cfg;
    private readonly ILLMProviderRegistry _reg;
    private readonly ILogger<LlmWarmupHostedService> _log;
    public LlmWarmupHostedService(ILlmConfigService cfg, ILLMProviderRegistry reg, ILogger<LlmWarmupHostedService> log)
    { _cfg = cfg; _reg = reg; _log = log; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (model, turbo) = await _cfg.GetActiveWithTurboAsync(cancellationToken);
            if (model?.WarmupOnStart == true)
            {
                var prov = _reg.GetProvider(model.Provider);
                if (prov is ILLMProviderConfigurable conf)
                {
                    conf.SetRuntimeConfig(model, turbo);
                    _log.LogInformation("Warming up LLM model {Name} ({Provider})...", model.Name, model.Provider);
                    await conf.WarmupAsync(cancellationToken);
                    _log.LogInformation("LLM model warmed up.");
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Warmup failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
