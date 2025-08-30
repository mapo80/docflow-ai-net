using System.Diagnostics;
using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","LspService")]
public class LspProxyServiceTests : IDisposable
{
    private readonly string _root;
    private readonly IWebHostEnvironment _env;

    public LspProxyServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_root);
        _env = new FakeEnv(_root);
    }

    [Fact]
    public void Ensure_workspace_creates_structure()
    {
        var svc = new LspProxyService(_env, new ConfigurationBuilder().Build());
        var dir = svc.EnsureWorkspace("ws1");
        File.Exists(Path.Combine(dir, "workspace.csproj")).Should().BeTrue();
        Directory.Exists(Path.Combine(dir, "lib")).Should().BeTrue();
    }

    [Fact]
    public async Task Sync_writes_file()
    {
        var svc = new LspProxyService(_env, new ConfigurationBuilder().Build());
        await svc.SyncAsync("ws2", "a.csx", "content", CancellationToken.None);
        var file = Path.Combine(_root, "workspace", "ws2", "a.csx");
        File.ReadAllText(file).Should().Be("content");
    }

    [Fact]
    public void Build_process_disabled_throws()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Lsp:Enabled"] = "false",
            ["Lsp:ServerPath"] = "/usr/bin/cat"
        }).Build();
        var svc = new LspProxyService(_env, cfg);
        Action act = () => svc.BuildServerProcess("/tmp");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_process_missing_path_throws()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Lsp:Enabled"] = "true",
            ["Lsp:ServerPath"] = ""
        }).Build();
        var svc = new LspProxyService(_env, cfg);
        Action act = () => svc.BuildServerProcess("/tmp");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_process_returns_info()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Lsp:Enabled"] = "true",
            ["Lsp:ServerPath"] = "/usr/bin/env",
            ["Lsp:Args:0"] = "bash"
        }).Build();
        var svc = new LspProxyService(_env, cfg);
        var psi = svc.BuildServerProcess("/tmp");
        psi.FileName.Should().Be("/usr/bin/env");
        psi.ArgumentList.Should().Contain("bash");
        psi.WorkingDirectory.Should().Be("/tmp");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch { }
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string root)
        {
            ContentRootPath = root;
            WebRootPath = root;
        }

        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
