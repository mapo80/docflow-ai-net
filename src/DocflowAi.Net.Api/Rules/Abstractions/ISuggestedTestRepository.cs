using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface ISuggestedTestRepository
{
    IReadOnlyList<SuggestedTest> GetByRule(Guid ruleId);
    IReadOnlyList<SuggestedTest> GetByIds(Guid ruleId, IEnumerable<Guid> ids);
    bool Exists(Guid ruleId, string hash);
    void Add(SuggestedTest suggestion);
    void Delete(Guid id);
    void SaveChanges();
}

