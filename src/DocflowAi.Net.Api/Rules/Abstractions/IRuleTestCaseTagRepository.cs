using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface IRuleTestCaseTagRepository
{
    IReadOnlyList<RuleTestCaseTag> GetByTest(Guid testId);
    void Add(RuleTestCaseTag link);
    void Delete(Guid testId, Guid tagId);
    void SaveChanges();
}

