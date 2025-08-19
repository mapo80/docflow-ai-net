using System.Text.Json;
using System.Text.RegularExpressions;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Api.Templates.Abstractions;
using DocflowAi.Net.Api.Templates.Models;

namespace DocflowAi.Net.Api.Templates.Services;

public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _repo;
    private static readonly Regex Slug = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public TemplateService(ITemplateRepository repo)
    {
        _repo = repo;
    }

    public PagedResult<TemplateSummary> GetPaged(string? q, int page, int pageSize, string? sort)
    {
        var (items, total) = _repo.GetPaged(q, page, pageSize, sort);
        var summaries = items.Select(ToSummary).ToList();
        return new PagedResult<TemplateSummary>(summaries, page < 1 ? 1 : page, NormalizePageSize(pageSize), total);
    }

    public TemplateDto? GetById(Guid id)
    {
        var t = _repo.GetById(id);
        return t == null ? null : ToDto(t);
    }

    public TemplateDto Create(CreateTemplateRequest request)
    {
        ValidateName(request.Name, null);
        ValidateToken(request.Token, null);
        if (request.FieldsJson.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("fieldsJson must be an array");

        var doc = new TemplateDocument
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Token = request.Token,
            PromptMarkdown = request.PromptMarkdown,
            FieldsJson = request.FieldsJson.GetRawText()
        };
        _repo.Add(doc);
        _repo.SaveChanges();
        return ToDto(doc);
    }

    public TemplateDto Update(Guid id, UpdateTemplateRequest request)
    {
        var existing = _repo.GetById(id) ?? throw new KeyNotFoundException("template not found");
        if (request.Name != null && request.Name != existing.Name)
        {
            ValidateName(request.Name, id);
            existing.Name = request.Name;
        }
        if (request.Token != null && request.Token != existing.Token)
        {
            ValidateToken(request.Token, id);
            existing.Token = request.Token;
        }
        if (request.PromptMarkdown != null)
            existing.PromptMarkdown = request.PromptMarkdown;
        if (request.FieldsJson.HasValue)
        {
            if (request.FieldsJson.Value.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("fieldsJson must be an array");
            existing.FieldsJson = request.FieldsJson.Value.GetRawText();
        }
        _repo.Update(existing);
        _repo.SaveChanges();
        return ToDto(existing);
    }

    public void Delete(Guid id)
    {
        var existing = _repo.GetById(id);
        if (existing == null) throw new KeyNotFoundException("template not found");
        _repo.Delete(id);
        _repo.SaveChanges();
    }

    private void ValidateName(string name, Guid? excludeId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new ArgumentException("invalid name");
        if (_repo.ExistsByName(name, excludeId))
            throw new InvalidOperationException("name exists");
    }

    private void ValidateToken(string token, Guid? excludeId)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100 || !Slug.IsMatch(token))
            throw new ArgumentException("invalid token");
        if (_repo.ExistsByToken(token, excludeId))
            throw new InvalidOperationException("token exists");
    }

    private static TemplateDto ToDto(TemplateDocument t) =>
        new(t.Id, t.Name, t.Token, t.PromptMarkdown, JsonDocument.Parse(t.FieldsJson).RootElement, t.CreatedAt, t.UpdatedAt);

    private static TemplateSummary ToSummary(TemplateDocument t) =>
        new(t.Id, t.Name, t.Token, t.CreatedAt, t.UpdatedAt);

    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? 20 : pageSize > 100 ? 100 : pageSize;
}
