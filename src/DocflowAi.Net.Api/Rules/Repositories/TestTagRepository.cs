using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class TestTagRepository : ITestTagRepository
{
    private readonly JobDbContext _db;
    public TestTagRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<TestTag> GetAll() =>
        _db.TestTags.OrderBy(x => x.Name).AsNoTracking().ToList();

    public TestTag? GetById(Guid id) => _db.TestTags.Find(id);

    public TestTag? GetByName(string name) =>
        _db.TestTags.AsNoTracking().FirstOrDefault(x => x.Name == name);

    public void Add(TestTag tag)
    {
        tag.UpdatedAt = DateTimeOffset.UtcNow;
        _db.TestTags.Add(tag);
    }

    public void Update(TestTag tag)
    {
        tag.UpdatedAt = DateTimeOffset.UtcNow;
        _db.TestTags.Update(tag);
    }

    public void Delete(Guid id)
    {
        var existing = _db.TestTags.Find(id);
        if (existing != null) _db.TestTags.Remove(existing);
    }

    public void SaveChanges() => _db.SaveChanges();
}

