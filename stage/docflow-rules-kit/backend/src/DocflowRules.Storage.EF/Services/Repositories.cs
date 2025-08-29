namespace DocflowRules.Storage.EF;

public interface IRuleFunctionRepository
{
    Task<List<RuleFunction>> GetAllAsync(CancellationToken ct = default);
    Task<RuleFunction?> GetAsync(Guid id, CancellationToken ct = default);
    Task<RuleFunction?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<RuleFunction> AddAsync(RuleFunction rule, CancellationToken ct = default);
    Task UpdateAsync(RuleFunction rule, CancellationToken ct = default);
}

public interface IRuleTestCaseRepository
{
    Task<List<RuleTestCase>> GetByRuleAsync(Guid ruleId, CancellationToken ct = default);
    Task<RuleTestCase> AddAsync(RuleTestCase test, CancellationToken ct = default);
    Task UpdateAsync(RuleTestCase test, CancellationToken ct = default);
}


public interface ITestSuiteRepository
{
    Task<List<TestSuite>> GetAllAsync(CancellationToken ct = default);
    Task<TestSuite?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TestSuite?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<TestSuite> AddAsync(TestSuite s, CancellationToken ct = default);
    Task UpdateAsync(TestSuite s, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ITestTagRepository
{
    Task<List<TestTag>> GetAllAsync(CancellationToken ct = default);
    Task<TestTag?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TestTag?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<TestTag> AddAsync(TestTag t, CancellationToken ct = default);
    Task UpdateAsync(TestTag t, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
