namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Service for managing model definitions.
/// </summary>
public interface IModelService
{
    IEnumerable<ModelDto> GetAll();
    ModelDto? GetById(Guid id);
    ModelDto Create(CreateModelRequest request);
    ModelDto Update(Guid id, UpdateModelRequest request);
    void Delete(Guid id);
    void StartDownload(Guid id);
    string GetDownloadLog(Guid id);
}
