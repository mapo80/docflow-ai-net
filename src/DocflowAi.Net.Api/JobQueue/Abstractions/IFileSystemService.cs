using Microsoft.AspNetCore.Http;

namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IFileSystemService
{
    void EnsureDirectory(string path);
    string CreateJobDirectory(Guid jobId);
    Task<string> SaveInputAtomic(Guid jobId, IFormFile file, CancellationToken ct = default);
    Task<string> SaveTextAtomic(Guid jobId, string filename, string content, CancellationToken ct = default);
}
