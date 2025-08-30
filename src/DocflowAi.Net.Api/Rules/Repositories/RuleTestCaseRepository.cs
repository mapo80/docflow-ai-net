using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class RuleTestCaseRepository : IRuleTestCaseRepository
{
    private readonly JobDbContext _db;
    public RuleTestCaseRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<RuleTestCase> GetByRule(Guid ruleId) =>
        _db.RuleTestCases
            .Where(t => t.RuleFunctionId == ruleId)
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToList();

    public void Add(RuleTestCase test)
    {
        test.UpdatedAt = DateTimeOffset.UtcNow;
        _db.RuleTestCases.Add(test);
    }

    public void Update(RuleTestCase test)
    {
        test.UpdatedAt = DateTimeOffset.UtcNow;
        _db.RuleTestCases.Update(test);
    }

    public void Remove(RuleTestCase test) => _db.RuleTestCases.Remove(test);

    public void SaveChanges() => _db.SaveChanges();
}

