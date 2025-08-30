using DocflowAi.Net.Api.Rules.Models;

namespace DocflowAi.Net.Api.Rules.Abstractions;

public interface ITestSuiteRepository
{
    IReadOnlyList<TestSuite> GetAll();
    TestSuite? GetById(Guid id);
    TestSuite? GetByName(string name);
    void Add(TestSuite suite);
    void Update(TestSuite suite);
    void Delete(Guid id);
    void SaveChanges();
}

