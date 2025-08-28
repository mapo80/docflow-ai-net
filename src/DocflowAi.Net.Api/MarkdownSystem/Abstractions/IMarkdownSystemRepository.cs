using DocflowAi.Net.Api.MarkdownSystem.Models;

namespace DocflowAi.Net.Api.MarkdownSystem.Abstractions;

public interface IMarkdownSystemRepository
{
    IEnumerable<MarkdownSystemDocument> GetAll();
    MarkdownSystemDocument? GetById(Guid id);
    MarkdownSystemDocument? GetByName(string name);
    void Add(MarkdownSystemDocument system, string? apiKey);
    void Update(MarkdownSystemDocument system, string? apiKey);
    void Delete(Guid id);
    bool ExistsByName(string name);
    void SaveChanges();
}
