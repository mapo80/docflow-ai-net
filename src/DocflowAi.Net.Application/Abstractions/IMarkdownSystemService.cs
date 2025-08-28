namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Service for managing markdown systems.</summary>
public interface IMarkdownSystemService
{
    IEnumerable<MarkdownSystemDto> GetAll();
    MarkdownSystemDto? GetById(Guid id);
    MarkdownSystemDto Create(CreateMarkdownSystemRequest request);
    MarkdownSystemDto Update(Guid id, UpdateMarkdownSystemRequest request);
    void Delete(Guid id);
}
