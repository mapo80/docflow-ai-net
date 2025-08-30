using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Rules.Services;

/// <summary>
/// Provides CRUD and cloning operations for test suites while delegating all data access to the repository layer.
/// </summary>
public class SuiteService
{
    private readonly ITestSuiteRepository _repo;
    private readonly ILogger<SuiteService> _log;

    public SuiteService(ITestSuiteRepository repo, ILogger<SuiteService> log)
    {
        _repo = repo;
        _log = log;
    }

    public Task<IReadOnlyList<TestSuite>> GetAllAsync(CancellationToken ct)
        => Task.FromResult(_repo.GetAll());

    public Task<(TestSuite? Suite, bool Conflict)> CreateAsync(string name, string? color, string? description, CancellationToken ct)
    {
        if (_repo.GetByName(name) != null)
            return Task.FromResult<(TestSuite?, bool)>((null, true));

        var suite = new TestSuite { Name = name, Color = color, Description = description };
        _repo.Add(suite);
        _repo.SaveChanges();
        _log.LogInformation("Suite created {Name}", suite.Name);
        return Task.FromResult<(TestSuite?, bool)>((suite, false));
    }

    public Task<bool> UpdateAsync(Guid id, string name, string? color, string? description, CancellationToken ct)
    {
        var suite = _repo.GetById(id);
        if (suite == null) return Task.FromResult(false);

        suite.Name = name;
        suite.Color = color;
        suite.Description = description;
        _repo.Update(suite);
        _repo.SaveChanges();
        _log.LogInformation("Suite updated {Id}", id);
        return Task.FromResult(true);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        _repo.Delete(id);
        _repo.SaveChanges();
        _log.LogWarning("Suite deleted {Id}", id);
        return Task.CompletedTask;
    }

    public Task<(TestSuite? Suite, bool NotFound, bool Conflict)> CloneAsync(Guid id, string newName, CancellationToken ct)
    {
        var existing = _repo.GetById(id);
        if (existing == null)
            return Task.FromResult<(TestSuite?, bool, bool)>((null, true, false));

        if (_repo.GetByName(newName) != null)
            return Task.FromResult<(TestSuite?, bool, bool)>((null, false, true));

        var dup = new TestSuite { Name = newName, Color = existing.Color, Description = existing.Description };
        _repo.Add(dup);
        _repo.SaveChanges();
        _log.LogInformation("Suite cloned {SourceId} -> {NewName}", id, newName);
        return Task.FromResult<(TestSuite?, bool, bool)>((dup, false, false));
    }
}

