using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface IRuleTestCaseRepository
{
    IReadOnlyList<RuleTestCase> GetByRule(Guid ruleId);
    void Add(RuleTestCase test);
    void Update(RuleTestCase test);
    void SaveChanges();
}

