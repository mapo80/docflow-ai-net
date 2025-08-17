using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DocflowAi.Net.Api.Features.Models;

/// <summary>
/// Coordinates "active model" state with DB and calls IModelActivator to hot switch.
/// </summary>
public class ModelRuntimeManager
{
    private readonly IDbContextFactory<ModelCatalogDbContext> _dbFactory;
    private readonly IModelActivator _activator;
    private readonly IModelActivator2? _activator2;
    private readonly ILogger<ModelRuntimeManager> _log;
    private readonly object _gate = new();

    public string? ActivePath { get; private set; }

    public ModelActivationPayload? ActivePayload { get; private set; }

    public ModelRuntimeManager(IDbContextFactory<ModelCatalogDbContext> dbFactory, IModelActivator activator, ILogger<ModelRuntimeManager> log, IModelActivator2? activator2 = null)
    {
        _dbFactory = dbFactory; _activator = activator; _log = log; _activator2 = activator2;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var active = await db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.IsActive, ct);
        if (active?.LocalPath is { Length: > 0 } path && File.Exists(path))
        {
            ActivePath = path;
            _log.LogInformation("ModelRuntimeManager initialized with active model: {Path}", path);
        }
    }

    
    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var model = await db.Models.FirstOrDefaultAsync(m => m.Id == id, ct)
                    ?? throw new InvalidOperationException("Model not found");

        ModelActivationPayload payload;

        switch (model.SourceType)
        {
            case ModelSourceType.Local:
            case ModelSourceType.Url:
            case ModelSourceType.HuggingFace:
                if (string.IsNullOrWhiteSpace(model.LocalPath) || !File.Exists(model.LocalPath))
                    throw new InvalidOperationException("Model is not available on disk");
                payload = new ModelActivationPayload(model.SourceType, model.LocalPath, null, null, null, null, null, null, null);
                break;

            case ModelSourceType.OpenAI:
                if (string.IsNullOrWhiteSpace(model.ApiKey) || string.IsNullOrWhiteSpace(model.Model))
                    throw new InvalidOperationException("OpenAI requires ApiKey and Model");
                payload = new ModelActivationPayload(model.SourceType, null, "https://api.openai.com/v1", model.ApiKey, model.Model, model.Organization, null, null, model.ExtraHeadersJson);
                break;

            case ModelSourceType.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(model.Endpoint) || string.IsNullOrWhiteSpace(model.ApiKey) || string.IsNullOrWhiteSpace(model.Deployment))
                    throw new InvalidOperationException("Azure OpenAI requires Endpoint, ApiKey and Deployment");
                payload = new ModelActivationPayload(model.SourceType, null, model.Endpoint, model.ApiKey, model.Model ?? model.Deployment, null, model.ApiVersion, model.Deployment, model.ExtraHeadersJson);
                break;

            case ModelSourceType.OpenAICompatible:
                if (string.IsNullOrWhiteSpace(model.Endpoint) || string.IsNullOrWhiteSpace(model.ApiKey) || string.IsNullOrWhiteSpace(model.Model))
                    throw new InvalidOperationException("OpenAI-compatible requires Endpoint, ApiKey and Model");
                payload = new ModelActivationPayload(model.SourceType, null, model.Endpoint, model.ApiKey, model.Model, model.Organization, model.ApiVersion, model.Deployment, model.ExtraHeadersJson);
                break;

            default:
                throw new InvalidOperationException("Unsupported source type");
        }

        lock (_gate) { /* serialize switching */ }
        _log.LogInformation("Activating model {Name} ({Type})", model.Name, model.SourceType.ToString());

        if (_activator2 is not null)
        {
            await _activator2.ActivateAsync(payload, ct);
        }
        else if (!string.IsNullOrWhiteSpace(payload.LocalPath))
        {
            await _activator.ActivateAsync(payload.LocalPath!, ct);
        }
        else
        {
            throw new InvalidOperationException("No activator available for non-local provider");
        }

        var current = await db.Models.Where(m => m.IsActive).ToListAsync(ct);
        foreach (var m in current) m.IsActive = false;

        model.IsActive = true;
        model.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        ActivePath = payload.LocalPath;
        ActivePayload = payload;
        _log.LogInformation("Model activated: {Name}", model.Name);
    }
}
