using DocflowAi.Net.Application.Configuration;
using DocflowAi.Net.Infrastructure.Markitdown;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DocflowAi.Net.Tests.Integration;

public class MarkitdownServiceFixture: IAsyncLifetime {
    Process? _proc; public string BaseUrl { get; private set; } = "http://127.0.0.1:8000"; readonly string _root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")); string? _err;
    public async Task InitializeAsync() {
        var start = new ProcessStartInfo("python", "-m uvicorn python.markitdown_service.main:app --host 127.0.0.1 --port 8000") { WorkingDirectory = _root, UseShellExecute = false, RedirectStandardError = true };
        start.Environment["PYTHONPATH"] = _root;
        _proc = Process.Start(start);
        _ = Task.Run(() => _err = _proc!.StandardError.ReadToEnd());
        using var http = new HttpClient();
        for (int i=0;i<60;i++) {
            try { var res = await http.GetAsync(BaseUrl+"/health"); if (res.IsSuccessStatusCode) return; } catch { }
            await Task.Delay(500);
        }
        throw new Exception("Markitdown service failed to start: " + _err);
    }
    public Task DisposeAsync() { if (_proc!=null && !_proc.HasExited) _proc.Kill(true); return Task.CompletedTask; }
}

public class MarkitdownClientTests: IClassFixture<MarkitdownServiceFixture> {
    readonly MarkitdownClient _client; readonly string _root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    public MarkitdownClientTests(MarkitdownServiceFixture fx) {
        var http = new HttpClient();
        var opts = Options.Create(new ServicesOptions{ Markitdown = new MarkitdownOptions{ BaseUrl = fx.BaseUrl, TimeoutSeconds = 30 }});
        _client = new MarkitdownClient(http, opts, NullLogger<MarkitdownClient>.Instance);
    }
    [Fact] public async Task Pdf_Ok() {
        var path = Path.Combine(_root, "dataset/sample_invoice.pdf");
        await using var fs = File.OpenRead(path);
        var md = await _client.ToMarkdownAsync(fs, "sample_invoice.pdf", default);
        md.Should().Contain("Invoice Number: INV-2025-001");
    }
    [Fact]
    public async Task Png_Handled()
    {
        var path = Path.Combine(_root, "dataset/sample_invoice.png");
        await using var fs = File.OpenRead(path);
        try
        {
            var md = await _client.ToMarkdownAsync(fs, "sample_invoice.png", default);
            md.Should().NotBeNull();
        }
        catch (MarkitdownException ex)
        {
            ex.Message.Should().NotBeNull();
        }
    }
    [Fact] public async Task UnsupportedExtension_Throws() {
        var path = Path.Combine(_root, "dataset/sample_invoice.pdf");
        await using var fs = File.OpenRead(path);
        await Assert.ThrowsAsync<MarkitdownException>(() => _client.ToMarkdownAsync(fs, "note.txt", default));
    }
    [Fact] public async Task EmptyFile_Throws() {
        await using var ms = new MemoryStream();
        await Assert.ThrowsAsync<MarkitdownException>(() => _client.ToMarkdownAsync(ms, "empty.pdf", default));
    }
}
