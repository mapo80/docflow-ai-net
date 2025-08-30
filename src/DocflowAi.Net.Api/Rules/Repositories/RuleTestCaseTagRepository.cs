using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class RuleTestCaseTagRepository : IRuleTestCaseTagRepository
{
    private readonly JobDbContext _db;
    public RuleTestCaseTagRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<RuleTestCaseTag> GetByTest(Guid testId) =>
        _db.RuleTestCaseTags
            .Where(x => x.RuleTestCaseId == testId)
            .AsNoTracking()
            .ToList();

    public void Add(RuleTestCaseTag link) => _db.RuleTestCaseTags.Add(link);

    public void Delete(Guid testId, Guid tagId)
    {
        var existing = _db.RuleTestCaseTags
            .FirstOrDefault(x => x.RuleTestCaseId == testId && x.TestTagId == tagId);
        if (existing != null) _db.RuleTestCaseTags.Remove(existing);
    }

    public void SaveChanges() => _db.SaveChanges();
}

