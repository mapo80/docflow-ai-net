using DocflowAi.Net.Api.Templates.Models;

namespace DocflowAi.Net.Api.Templates.Abstractions;

public interface ITemplateRepository
{
    (IReadOnlyList<TemplateDocument> items, int total) GetPaged(string? q, int page, int pageSize, string? sort);
    TemplateDocument? GetById(Guid id);
    bool ExistsByName(string name, Guid? excludeId = null);
    bool ExistsByToken(string token, Guid? excludeId = null);
    void Add(TemplateDocument template);
    void Update(TemplateDocument template);
    void Delete(Guid id);
    void SaveChanges();
}
