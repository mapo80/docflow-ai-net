using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Api.Services;

public interface ILlmConfigService
{
    Task<LlmModel?> GetActiveAsync(CancellationToken ct);
    Task<(LlmModel? model, bool turbo)> GetActiveWithTurboAsync(CancellationToken ct);
    Task<LlmModel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<LlmModel>> ListAsync(CancellationToken ct);
    Task<LlmModel> CreateAsync(LlmModel model, CancellationToken ct);
    Task<LlmModel> UpdateAsync(LlmModel model, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task SetActiveAsync(Guid id, bool turbo, CancellationToken ct);
}

public class LlmConfigService : ILlmConfigService
{
    private readonly AppDbContext _db;
    public LlmConfigService(AppDbContext db) { _db = db; }

    public async Task<LlmModel?> GetActiveAsync(CancellationToken ct)
        => await GetActiveWithTurboAsync(ct).ContinueWith(t => t.Result.model, ct);

    public async Task<(LlmModel? model, bool turbo)> GetActiveWithTurboAsync(CancellationToken ct)
    {
        var set = await _db.LlmSettings.FirstOrDefaultAsync(ct) ?? new LlmSettings();
        if (set.Id == 0) { set.Id = 1; _db.LlmSettings.Add(set); await _db.SaveChangesAsync(ct); }
        if (set.ActiveModelId == null) return (await _db.LlmModels.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct), set.TurboProfile);
        var m = await _db.LlmModels.FirstOrDefaultAsync(x => x.Id == set.ActiveModelId, ct);
        return (m, set.TurboProfile);
    }

    public Task<LlmModel?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.LlmModels.FirstOrDefaultAsync(x => x.Id == id, ct)!;

    public Task<List<LlmModel>> ListAsync(CancellationToken ct)
        => _db.LlmModels.OrderByDescending(x => x.UpdatedAt).ToListAsync(ct);

    public async Task<LlmModel> CreateAsync(LlmModel model, CancellationToken ct)
    {
        model.Id = Guid.NewGuid();
        model.CreatedAt = DateTimeOffset.UtcNow;
        model.UpdatedAt = model.CreatedAt;
        _db.LlmModels.Add(model);
        await _db.SaveChangesAsync(ct);
        return model;
    }

    public async Task<LlmModel> UpdateAsync(LlmModel model, CancellationToken ct)
    {
        var dbm = await _db.LlmModels.FirstAsync(x => x.Id == model.Id, ct);
        dbm.Provider = model.Provider;
        dbm.Name = model.Name;
        dbm.ModelPathOrId = model.ModelPathOrId;
        dbm.Endpoint = model.Endpoint;
        dbm.ApiKey = model.ApiKey;
        dbm.ContextSize = model.ContextSize;
        dbm.Threads = model.Threads;
        dbm.BatchSize = model.BatchSize;
        dbm.MaxTokens = model.MaxTokens;
        dbm.Temperature = model.Temperature;
        dbm.WarmupOnStart = model.WarmupOnStart;
        dbm.Enabled = model.Enabled;
        dbm.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return dbm;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var m = await _db.LlmModels.FirstAsync(x => x.Id == id, ct);
        _db.LlmModels.Remove(m);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, bool turbo, CancellationToken ct)
    {
        var set = await _db.LlmSettings.FirstOrDefaultAsync(ct) ?? new LlmSettings { Id = 1 };
        set.ActiveModelId = id;
        set.TurboProfile = turbo;
        set.UpdatedAt = DateTimeOffset.UtcNow;
        _db.LlmSettings.Update(set);
        await _db.SaveChangesAsync(ct);
    }
}

public interface ILLMProviderConfigurable
{
    void SetRuntimeConfig(LlmModel model, bool turbo);
    Task WarmupAsync(CancellationToken ct);
}

public interface ILLMProviderRegistry
{
    DocflowRules.Api.Services.ILLMProvider GetProvider(string providerName);
}

public class LlmProviderRegistry : ILLMProviderRegistry
{
    private readonly IServiceProvider _sp;
    public LlmProviderRegistry(IServiceProvider sp) { _sp = sp; }

    public ILLMProvider GetProvider(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => _sp.GetRequiredService<DocflowRules.Api.LLM.OpenAiProvider>(),
            "LlamaSharp" or "Local" => _sp.GetRequiredService<DocflowRules.Api.LLM.LlamaSharpProvider>(),
            _ => _sp.GetRequiredService<DocflowRules.Api.Services.MockLLMProvider>()
        };
    }
}
