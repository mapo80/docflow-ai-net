using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class TestSuiteRepository : ITestSuiteRepository
{
    private readonly JobDbContext _db;
    public TestSuiteRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<TestSuite> GetAll() =>
        _db.TestSuites.OrderBy(x => x.Name).AsNoTracking().ToList();

    public TestSuite? GetById(Guid id) => _db.TestSuites.Find(id);

    public TestSuite? GetByName(string name) =>
        _db.TestSuites.AsNoTracking().FirstOrDefault(x => x.Name == name);

    public void Add(TestSuite suite)
    {
        suite.UpdatedAt = DateTimeOffset.UtcNow;
        _db.TestSuites.Add(suite);
    }

    public void Update(TestSuite suite)
    {
        suite.UpdatedAt = DateTimeOffset.UtcNow;
        _db.TestSuites.Update(suite);
    }

    public void Delete(Guid id)
    {
        var existing = _db.TestSuites.Find(id);
        if (existing != null) _db.TestSuites.Remove(existing);
    }

    public void SaveChanges() => _db.SaveChanges();
}

