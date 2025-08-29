using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Storage.EF;

public class RuleFunctionRepository : IRuleFunctionRepository
{
    private readonly AppDbContext _db;
    public RuleFunctionRepository(AppDbContext db) => _db = db;

    public Task<List<RuleFunction>> GetAllAsync(CancellationToken ct = default)
        => _db.RuleFunctions.OrderByDescending(r => r.IsBuiltin).ThenBy(r => r.Name).ToListAsync(ct);

    public Task<RuleFunction?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.RuleFunctions.FindAsync(new object?[] { id }, ct).AsTask();

    public Task<RuleFunction?> GetByNameAsync(string name, CancellationToken ct = default)
        => _db.RuleFunctions.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<RuleFunction> AddAsync(RuleFunction rule, CancellationToken ct = default)
    {
        _db.RuleFunctions.Add(rule);
        await _db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task UpdateAsync(RuleFunction rule, CancellationToken ct = default)
    {
        _db.RuleFunctions.Update(rule);
        await _db.SaveChangesAsync(ct);
    }
}

public class RuleTestCaseRepository : IRuleTestCaseRepository
{
    private readonly AppDbContext _db;
    public RuleTestCaseRepository(AppDbContext db) => _db = db;

    public Task<List<RuleTestCase>> GetByRuleAsync(Guid ruleId, CancellationToken ct = default)
        => _db.RuleTestCases.Where(t => t.RuleFunctionId == ruleId).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<RuleTestCase> AddAsync(RuleTestCase test, CancellationToken ct = default)
    {
        _db.RuleTestCases.Add(test);
        await _db.SaveChangesAsync(ct);
        return test;
    }

    public async Task UpdateAsync(RuleTestCase test, CancellationToken ct = default)
    {
        _db.RuleTestCases.Update(test);
        await _db.SaveChangesAsync(ct);
    }
}


public class TestSuiteRepository : ITestSuiteRepository
{
    private readonly AppDbContext _db; public TestSuiteRepository(AppDbContext db)=>_db=db;
    public Task<List<TestSuite>> GetAllAsync(CancellationToken ct=default) => _db.TestSuites.OrderBy(x=>x.Name).ToListAsync(ct);
    public Task<TestSuite?> GetByIdAsync(Guid id, CancellationToken ct=default) => _db.TestSuites.FindAsync(new object?[]{id}, ct).AsTask();
    public Task<TestSuite?> GetByNameAsync(string name, CancellationToken ct=default) => _db.TestSuites.FirstOrDefaultAsync(x=>x.Name==name, ct);
    public async Task<TestSuite> AddAsync(TestSuite s, CancellationToken ct=default){_db.TestSuites.Add(s); await _db.SaveChangesAsync(ct); return s;}
    public async Task UpdateAsync(TestSuite s, CancellationToken ct=default){_db.TestSuites.Update(s); await _db.SaveChangesAsync(ct);}    
    public async Task DeleteAsync(Guid id, CancellationToken ct=default){var e=await GetByIdAsync(id,ct); if(e!=null){_db.TestSuites.Remove(e); await _db.SaveChangesAsync(ct);} }
}

public class TestTagRepository : ITestTagRepository
{
    private readonly AppDbContext _db; public TestTagRepository(AppDbContext db)=>_db=db;
    public Task<List<TestTag>> GetAllAsync(CancellationToken ct=default) => _db.TestTags.OrderBy(x=>x.Name).ToListAsync(ct);
    public Task<TestTag?> GetByIdAsync(Guid id, CancellationToken ct=default) => _db.TestTags.FindAsync(new object?[]{id}, ct).AsTask();
    public Task<TestTag?> GetByNameAsync(string name, CancellationToken ct=default) => _db.TestTags.FirstOrDefaultAsync(x=>x.Name==name, ct);
    public async Task<TestTag> AddAsync(TestTag t, CancellationToken ct=default){_db.TestTags.Add(t); await _db.SaveChangesAsync(ct); return t;}
    public async Task UpdateAsync(TestTag t, CancellationToken ct=default){_db.TestTags.Update(t); await _db.SaveChangesAsync(ct);}    
    public async Task DeleteAsync(Guid id, CancellationToken ct=default){var e=await GetByIdAsync(id,ct); if(e!=null){_db.TestTags.Remove(e); await _db.SaveChangesAsync(ct);} }
}
