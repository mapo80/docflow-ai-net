using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface ITestTagRepository
{
    IReadOnlyList<TestTag> GetAll();
    TestTag? GetById(Guid id);
    TestTag? GetByName(string name);
    void Add(TestTag tag);
    void Update(TestTag tag);
    void Delete(Guid id);
    void SaveChanges();
}

