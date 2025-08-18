using DocflowAi.Net.Api.Model.Models;

namespace DocflowAi.Net.Api.Model.Abstractions;

public interface IModelRepository
{
    IEnumerable<ModelDocument> GetAll();
    ModelDocument? GetById(Guid id);
    ModelDocument? GetByName(string name);
    void Add(ModelDocument model, string? apiKey, string? hfToken);
    void Update(ModelDocument model, string? apiKey, string? hfToken);
    void Delete(Guid id);
    bool ExistsByName(string name);
    void SetDownloadStatus(Guid id, string status);
    void SetDownloaded(Guid id, bool downloaded, string? localPath = null, long? fileSize = null, string? checksum = null);
    void SetDownloadLogPath(Guid id, string path);
    string? GetHfToken(Guid id);
    void TouchLastUsed(Guid id);
    void SaveChanges();
}
