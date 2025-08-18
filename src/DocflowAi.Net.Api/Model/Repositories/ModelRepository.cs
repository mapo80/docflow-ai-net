using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Model.Abstractions;
using DocflowAi.Net.Api.Model.Models;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Model.Repositories;

public class ModelRepository : IModelRepository
{
    private readonly JobDbContext _db;
    private readonly ISecretProtector _protector;

    public ModelRepository(JobDbContext db, ISecretProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    public IEnumerable<ModelDocument> GetAll() => _db.Models.AsNoTracking().ToList();

    public ModelDocument? GetById(Guid id) => _db.Models.Find(id);

    public void Add(ModelDocument model, string? apiKey, string? hfToken)
    {
        model.ApiKeyEncrypted = apiKey != null ? _protector.Protect(apiKey) : null;
        model.HfTokenEncrypted = hfToken != null ? _protector.Protect(hfToken) : null;
        model.CreatedAt = model.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Models.Add(model);
    }

    public void Update(ModelDocument model, string? apiKey, string? hfToken)
    {
        var existing = _db.Models.Find(model.Id);
        if (existing == null) return;
        existing.Name = model.Name;
        existing.Type = model.Type;
        existing.Provider = model.Provider;
        existing.BaseUrl = model.BaseUrl;
        existing.HfRepo = model.HfRepo;
        existing.ModelFile = model.ModelFile;
        existing.IsActive = model.IsActive;
        if (apiKey != null) existing.ApiKeyEncrypted = _protector.Protect(apiKey);
        if (hfToken != null) existing.HfTokenEncrypted = _protector.Protect(hfToken);
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete(Guid id)
    {
        var existing = _db.Models.Find(id);
        if (existing != null)
            _db.Models.Remove(existing);
    }

    public bool ExistsByName(string name) => _db.Models.Any(m => m.Name == name);

    public void SetDownloadStatus(Guid id, string status)
    {
        var model = _db.Models.Find(id);
        if (model == null) return;
        model.DownloadStatus = status;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDownloaded(Guid id, bool downloaded, string? localPath = null, long? fileSize = null, string? checksum = null)
    {
        var model = _db.Models.Find(id);
        if (model == null) return;
        model.Downloaded = downloaded;
        model.LocalPath = localPath;
        model.FileSizeBytes = fileSize;
        model.Checksum = checksum;
        model.DownloadedAt = DateTimeOffset.UtcNow;
        model.UpdatedAt = DateTimeOffset.UtcNow;
        model.DownloadStatus = downloaded ? "Downloaded" : model.DownloadStatus;
    }

    public void SetDownloadLogPath(Guid id, string path)
    {
        var model = _db.Models.Find(id);
        if (model == null) return;
        model.DownloadLogPath = path;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string? GetHfToken(Guid id)
    {
        var model = _db.Models.AsNoTracking().FirstOrDefault(m => m.Id == id);
        if (model?.HfTokenEncrypted == null) return null;
        return _protector.Unprotect(model.HfTokenEncrypted);
    }

    public void TouchLastUsed(Guid id)
    {
        var model = _db.Models.Find(id);
        if (model == null) return;
        model.LastUsedAt = DateTimeOffset.UtcNow;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SaveChanges() => _db.SaveChanges();
}
