using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Api.Templates.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Templates.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly JobDbContext _db;

    public TemplateRepository(JobDbContext db)
    {
        _db = db;
    }

    public (IReadOnlyList<TemplateDocument> items, int total) GetPaged(string? q, int page, int pageSize, string? sort)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        var query = _db.Templates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var ql = q.ToLowerInvariant();
            query = query.Where(t => t.Name.ToLower().Contains(ql) || t.Token.ToLower().Contains(ql));
        }
        bool desc = true;
        string field = "createdAt";
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) field = parts[0];
            if (parts.Length > 1) desc = parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        }
        query = field switch
        {
            "name" => desc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "token" => desc ? query.OrderByDescending(t => t.Token) : query.OrderBy(t => t.Token),
            _ => desc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
        };
        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToList();
        return (items, total);
    }

    public TemplateDocument? GetById(Guid id) => _db.Templates.Find(id);

    public TemplateDocument? GetByToken(string token) =>
        _db.Templates.FirstOrDefault(t => t.Token == token);

    public bool ExistsByName(string name, Guid? excludeId = null) =>
        _db.Templates.Any(t => t.Name == name && (!excludeId.HasValue || t.Id != excludeId));

    public bool ExistsByToken(string token, Guid? excludeId = null) =>
        _db.Templates.Any(t => t.Token == token && (!excludeId.HasValue || t.Id != excludeId));

    public void Add(TemplateDocument template)
    {
        template.CreatedAt = template.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Templates.Add(template);
    }

    public void Update(TemplateDocument template)
    {
        template.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Templates.Update(template);
    }

    public void Delete(Guid id)
    {
        var existing = _db.Templates.Find(id);
        if (existing != null) _db.Templates.Remove(existing);
    }

    public void SaveChanges() => _db.SaveChanges();
}
