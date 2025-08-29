using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using DocflowAi.Net.Api.MarkdownSystem.Models;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Api.MarkdownSystem.Services;

/// <summary>Application service for managing markdown systems.</summary>
public class MarkdownSystemService : IMarkdownSystemService
{
    private readonly IMarkdownSystemRepository _repo;

    public MarkdownSystemService(IMarkdownSystemRepository repo) => _repo = repo;

    public IEnumerable<MarkdownSystemDto> GetAll() => _repo.GetAll().Select(ToDto);

    public MarkdownSystemDto? GetById(Guid id)
    {
        var system = _repo.GetById(id);
        return system == null ? null : ToDto(system);
    }

    public MarkdownSystemDto Create(CreateMarkdownSystemRequest request)
    {
        if (_repo.ExistsByName(request.Name))
            throw new InvalidOperationException("name exists");
        var doc = new MarkdownSystemDocument
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Provider = request.Provider,
            Endpoint = request.Endpoint,
        };
        _repo.Add(doc, request.ApiKey);
        _repo.SaveChanges();
        return ToDto(doc);
    }

    public MarkdownSystemDto Update(Guid id, UpdateMarkdownSystemRequest request)
    {
        var existing = _repo.GetById(id) ?? throw new InvalidOperationException("not found");
        existing.Name = request.Name;
        existing.Provider = request.Provider;
        existing.Endpoint = request.Endpoint;
        _repo.Update(existing, request.ApiKey);
        _repo.SaveChanges();
        return ToDto(existing);
    }

    public void Delete(Guid id)
    {
        _repo.Delete(id);
        _repo.SaveChanges();
    }

    private static MarkdownSystemDto ToDto(MarkdownSystemDocument d) =>
        new(d.Id, d.Name, d.Provider, d.Endpoint, d.ApiKeyEncrypted != null, d.CreatedAt, d.UpdatedAt);
}
