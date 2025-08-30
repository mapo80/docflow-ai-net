using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Rules.Repositories;

public class RuleFunctionRepository : IRuleFunctionRepository
{
    private readonly JobDbContext _db;
    public RuleFunctionRepository(JobDbContext db) => _db = db;

    public IReadOnlyList<RuleFunction> GetAll() =>
        _db.RuleFunctions
            .OrderByDescending(r => r.IsBuiltin)
            .ThenBy(r => r.Name)
            .AsNoTracking()
            .ToList();

    public RuleFunction? GetById(Guid id) => _db.RuleFunctions.Find(id);

    public RuleFunction? GetByName(string name) =>
        _db.RuleFunctions.AsNoTracking().FirstOrDefault(r => r.Name == name);

    public void Add(RuleFunction rule)
    {
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        _db.RuleFunctions.Add(rule);
    }

    public void Update(RuleFunction rule)
    {
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        _db.RuleFunctions.Update(rule);
    }

    public void SaveChanges() => _db.SaveChanges();
}

