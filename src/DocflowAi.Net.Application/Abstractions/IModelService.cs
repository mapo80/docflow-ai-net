namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Service for managing model definitions.
/// </summary>
public interface IModelService
{
    IEnumerable<ModelDto> GetAll();
    ModelDto? GetById(Guid id);
    ModelDto Create(CreateModelRequest request);
    void StartDownload(Guid id);
    string GetDownloadLog(Guid id);
}
