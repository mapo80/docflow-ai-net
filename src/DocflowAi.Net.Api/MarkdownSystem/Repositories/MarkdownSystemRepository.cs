using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using DocflowAi.Net.Api.MarkdownSystem.Models;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.MarkdownSystem.Repositories;

public class MarkdownSystemRepository : IMarkdownSystemRepository
{
    private readonly JobDbContext _db;
    private readonly ISecretProtector _protector;

    public MarkdownSystemRepository(JobDbContext db, ISecretProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    public IEnumerable<MarkdownSystemDocument> GetAll() => _db.MarkdownSystems.AsNoTracking().ToList();

    public MarkdownSystemDocument? GetById(Guid id) => _db.MarkdownSystems.Find(id);

    public MarkdownSystemDocument? GetByName(string name) =>
        _db.MarkdownSystems.AsNoTracking().FirstOrDefault(m => m.Name == name);

    public void Add(MarkdownSystemDocument system, string? apiKey)
    {
        system.ApiKeyEncrypted = apiKey != null ? _protector.Protect(apiKey) : null;
        system.CreatedAt = system.UpdatedAt = DateTimeOffset.UtcNow;
        _db.MarkdownSystems.Add(system);
    }

    public void Update(MarkdownSystemDocument system, string? apiKey)
    {
        var existing = _db.MarkdownSystems.Find(system.Id);
        if (existing == null) return;
        existing.Name = system.Name;
        existing.Provider = system.Provider;
        existing.Endpoint = system.Endpoint;
        if (apiKey != null) existing.ApiKeyEncrypted = _protector.Protect(apiKey);
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete(Guid id)
    {
        var existing = _db.MarkdownSystems.Find(id);
        if (existing != null)
            _db.MarkdownSystems.Remove(existing);
    }

    public bool ExistsByName(string name) => _db.MarkdownSystems.Any(m => m.Name == name);

    public void SaveChanges() => _db.SaveChanges();
}
