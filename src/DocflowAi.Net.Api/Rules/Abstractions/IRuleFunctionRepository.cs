using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface IRuleFunctionRepository
{
    IReadOnlyList<RuleFunction> GetAll();
    RuleFunction? GetById(Guid id);
    RuleFunction? GetByName(string name);
    void Add(RuleFunction rule);
    void Update(RuleFunction rule);
    void SaveChanges();
}

