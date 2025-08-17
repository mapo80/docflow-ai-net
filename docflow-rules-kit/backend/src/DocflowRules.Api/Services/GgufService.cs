using System.Diagnostics;
using System.Net.Http.Headers;
using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace DocflowRules.Api.Services;

public interface IGgufService
{
    Task<GgufDownloadJob> EnqueueDownloadAsync(string repo, string file, string? revision, CancellationToken ct);
    Task<GgufDownloadJob?> GetJobAsync(Guid id, CancellationToken ct);
    Task<List<GgufDownloadJob>> ListJobsAsync(CancellationToken ct);
    Task<List<object>> ListAvailableAsync(CancellationToken ct);
    Task<bool> DeleteAvailableAsync(string path, CancellationToken ct);
}

public class GgufService : IGgufService, IHostedService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<GgufService> _log;
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private CancellationTokenSource? _cts;
    private Task? _worker;

    public GgufService(AppDbContext db, IConfiguration cfg, IHttpClientFactory http, ILogger<GgufService> log)
    { _db = db; _cfg = cfg; _http = http; _log = log; }

    public async Task<GgufDownloadJob> EnqueueDownloadAsync(string repo, string file, string? revision, CancellationToken ct)
    {
        var job = new GgufDownloadJob { Repo = repo.Trim(), File = file.Trim(), Revision = string.IsNullOrWhiteSpace(revision) ? "main" : revision!.Trim(), Status = "queued" };
        _db.GgufDownloadJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        await _queue.Writer.WriteAsync(job.Id, ct);
        return job;
    }

    public Task<GgufDownloadJob?> GetJobAsync(Guid id, CancellationToken ct)
        => _db.GgufDownloadJobs.FirstOrDefaultAsync(x => x.Id == id, ct)!;

    public Task<List<GgufDownloadJob>> ListJobsAsync(CancellationToken ct)
        => _db.GgufDownloadJobs.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);

    public Task<List<object>> ListAvailableAsync(CancellationToken ct)
    {
        var dir = _cfg["LLM:Local:ModelsDir"] ?? "models";
        Directory.CreateDirectory(dir);
        var list = new List<object>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.gguf", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(file);
            list.Add(new { name = fi.Name, path = Path.GetFullPath(file), size = fi.Length, modified = fi.LastWriteTimeUtc });
        }
        return Task.FromResult(list);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _worker = Task.Run(() => WorkerAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        try { if (_worker != null) await _worker; } catch {}
    }

    private async Task WorkerAsync(CancellationToken ct)
    {
        var token = _cfg["HF:Token"];
        var dir = _cfg["LLM:Local:ModelsDir"] ?? "models";
        Directory.CreateDirectory(dir);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var id = await _queue.Reader.ReadAsync(ct);
                var job = await _db.GgufDownloadJobs.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (job == null) continue;

                job.Status = "running"; job.Progress = 0; job.Error = null; job.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);

                var url = $"https://huggingface.co/{job.Repo}/resolve/{job.Revision}/{job.File}?download=1";
                var temp = Path.Combine(dir, $".{job.Id}.part");
                var dest = Path.Combine(dir, job.File);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                var http = _http.CreateClient("huggingface");
                if (!string.IsNullOrWhiteSpace(token)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                var total = resp.Content.Headers.ContentLength ?? -1L;
                await using var input = await resp.Content.ReadAsStreamAsync(ct);
                await using var output = File.Create(temp);

                var buffer = new byte[1<<20];
                long read = 0;
                int n;
                var lastReport = DateTimeOffset.UtcNow;
                while ((n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, n), ct);
                    read += n;
                    var now = DateTimeOffset.UtcNow;
                    if (total > 0 && (now - lastReport).TotalMilliseconds > 500)
                    {
                        job.Progress = (int)Math.Round(read * 100.0 / total);
                        job.UpdatedAt = now;
                        await _db.SaveChangesAsync(ct);
                        lastReport = now;
                    }
                }
                output.Close();
                File.Move(temp, dest, true);

                job.Status = "succeeded"; job.Progress = 100; job.FilePath = Path.GetFullPath(dest); job.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                try
                {
                    var job = _db.GgufDownloadJobs.OrderByDescending(x=>x.CreatedAt).FirstOrDefault(x=>x.Status=="running");
                    if (job != null)
                    {
                        job.Status = "failed";
                        job.Error = ex.Message;
                        job.UpdatedAt = DateTimeOffset.UtcNow;
                        _db.SaveChanges();
                    }
                } catch {}
                await Task.Delay(500, ct);
            }
        }
    }
}

    public async Task<bool> DeleteAvailableAsync(string path, CancellationToken ct)
    {
        var dir = _cfg["LLM:Local:ModelsDir"] ?? "models";
        Directory.CreateDirectory(dir);
        var full = Path.GetFullPath(path);
        var root = Path.GetFullPath(dir) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Percorso non consentito");
        if (!File.Exists(full)) return false;

        // prevent deletion if referenced by enabled LlmModel
        var used = await _db.LlmModels.Where(m => m.Provider == "LlamaSharp" && m.Enabled && (m.ModelPathOrId ?? "") == full).AnyAsync(ct);
        if (used) throw new InvalidOperationException("File in uso da un modello abilitato. Disabilita o modifica il modello prima di cancellare.");

        // also block if active in settings
        var set = await _db.LlmSettings.FirstOrDefaultAsync(ct);
        if (set?.ActiveModelId != null)
        {
            var active = await _db.LlmModels.FirstOrDefaultAsync(m => m.Id == set.ActiveModelId, ct);
            if (active?.Provider == "LlamaSharp" && (active.ModelPathOrId ?? "") == full)
                throw new InvalidOperationException("File in uso dal modello attivo. Disattiva o modifica il modello attivo prima di cancellare.");
        }

        File.Delete(full);
        return true;
    }
