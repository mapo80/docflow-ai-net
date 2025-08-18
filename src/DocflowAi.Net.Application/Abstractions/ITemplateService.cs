namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Service for managing templates.
/// </summary>
public interface ITemplateService
{
    PagedResult<TemplateSummary> GetPaged(string? q, int page, int pageSize, string? sort);
    TemplateDto? GetById(Guid id);
    TemplateDto Create(CreateTemplateRequest request);
    TemplateDto Update(Guid id, UpdateTemplateRequest request);
    void Delete(Guid id);
}
