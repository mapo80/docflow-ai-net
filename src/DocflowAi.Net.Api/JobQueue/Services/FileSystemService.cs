using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocflowAi.Net.Api.JobQueue.Services;

public class FileSystemService : IFileSystemService
{
    private readonly JobQueueOptions _options;
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemService(IOptions<JobQueueOptions> options, ILogger<FileSystemService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);

    public string CreateJobDirectory(Guid jobId)
    {
        var dir = Path.Combine(_options.DataRoot, jobId.ToString("N"));
        Directory.CreateDirectory(dir);
        _logger.LogInformation("CreateJobDirectory {JobId} {Path}", jobId, dir);
        return dir;
    }

    public async Task<string> SaveInputAtomic(Guid jobId, IFormFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.FileName);
        var dir = Path.Combine(_options.DataRoot, jobId.ToString("N"));
        var path = Path.Combine(dir, "input" + ext);
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(fs, ct);
        _logger.LogInformation("SaveInputAtomic {JobId} {Bytes} {FileExt} {Path}", jobId, file.Length, ext, path);
        return path;
    }

    public async Task<string> SaveTextAtomic(Guid jobId, string filename, string content, CancellationToken ct = default)
    {
        var dir = Path.Combine(_options.DataRoot, jobId.ToString("N"));
        var path = Path.Combine(dir, filename);
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, content, ct);
        File.Move(tmp, path, true);
        _logger.LogDebug("SaveTextAtomic {JobId} {Path}", jobId, path);
        return path;
    }
}

