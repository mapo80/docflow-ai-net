using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class SuggestedTestRepository : ISuggestedTestRepository
{
    private readonly JobDbContext _db;
    public SuggestedTestRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<SuggestedTest> GetByRule(Guid ruleId) =>
        _db.SuggestedTests
            .Where(x => x.RuleId == ruleId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToList();

    public IReadOnlyList<SuggestedTest> GetByIds(Guid ruleId, IEnumerable<Guid> ids) =>
        _db.SuggestedTests
            .Where(x => x.RuleId == ruleId && ids.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToList();

    public bool Exists(Guid ruleId, string hash) =>
        _db.SuggestedTests.Any(x => x.RuleId == ruleId && x.Hash == hash);

    public void Add(SuggestedTest suggestion)
    {
        suggestion.CreatedAt = DateTimeOffset.UtcNow;
        _db.SuggestedTests.Add(suggestion);
    }

    public void Delete(Guid id)
    {
        var existing = _db.SuggestedTests.Find(id);
        if (existing != null) _db.SuggestedTests.Remove(existing);
    }

    public void SaveChanges() => _db.SaveChanges();
}

