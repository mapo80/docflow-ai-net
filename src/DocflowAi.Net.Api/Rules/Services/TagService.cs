using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Rules.Services;

/// <summary>
/// Provides CRUD operations for test tags while delegating all data access to the repository layer.
/// </summary>
public class TagService
{
    private readonly ITestTagRepository _repo;
    private readonly ILogger<TagService> _log;

    public TagService(ITestTagRepository repo, ILogger<TagService> log)
    {
        _repo = repo;
        _log = log;
    }

    public Task<IReadOnlyList<TestTag>> GetAllAsync(CancellationToken ct)
        => Task.FromResult(_repo.GetAll());

    public Task<(TestTag? Tag, bool Conflict)> CreateAsync(string name, string? color, string? description, CancellationToken ct)
    {
        if (_repo.GetByName(name) != null)
            return Task.FromResult<(TestTag?, bool)>((null, true));

        var tag = new TestTag { Name = name, Color = color, Description = description };
        _repo.Add(tag);
        _repo.SaveChanges();
        _log.LogInformation("Tag created {Name}", tag.Name);
        return Task.FromResult<(TestTag?, bool)>((tag, false));
    }

    public Task<bool> UpdateAsync(Guid id, string name, string? color, string? description, CancellationToken ct)
    {
        var tag = _repo.GetById(id);
        if (tag == null) return Task.FromResult(false);

        tag.Name = name;
        tag.Color = color;
        tag.Description = description;
        _repo.Update(tag);
        _repo.SaveChanges();
        _log.LogInformation("Tag updated {Id}", id);
        return Task.FromResult(true);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        _repo.Delete(id);
        _repo.SaveChanges();
        _log.LogWarning("Tag deleted {Id}", id);
        return Task.CompletedTask;
    }
}
