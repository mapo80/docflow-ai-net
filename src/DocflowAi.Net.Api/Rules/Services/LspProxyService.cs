using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DocflowAi.Net.Api.Rules.Services;

public class LspProxyService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;

    public LspProxyService(IWebHostEnvironment env, IConfiguration cfg)
    {
        _env = env;
        _cfg = cfg;
    }

    public string EnsureWorkspace(string? workspaceId)
    {
        var root = Path.Combine(_env.ContentRootPath, "workspace");
        var dir = Path.Combine(root, workspaceId ?? "default");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "lib"));

        var sdkDll = Path.Combine(AppContext.BaseDirectory, "DocflowRules.Sdk.dll");
        var targetDll = Path.Combine(dir, "lib", "DocflowRules.Sdk.dll");
        if (File.Exists(sdkDll) && !File.Exists(targetDll))
            File.Copy(sdkDll, targetDll, true);

        var csproj = Path.Combine(dir, "workspace.csproj");
        if (!File.Exists(csproj))
        {
            File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""DocflowRules.Sdk"">
      <HintPath>lib/DocflowRules.Sdk.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>");
        }

        return dir;
    }

    public async Task SyncAsync(string workspaceId, string? filePath, string? content, CancellationToken ct)
    {
        var dir = EnsureWorkspace(workspaceId);
        var file = Path.Combine(dir, filePath ?? "user.csx");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await File.WriteAllTextAsync(file, content ?? string.Empty, ct);
    }

    public ProcessStartInfo BuildServerProcess(string workspaceRoot)
    {
        var cfg = _cfg.GetSection("Lsp");
        var enabled = cfg.GetValue("Enabled", false);
        var path = cfg.GetValue<string?>("ServerPath");
        var args = cfg.GetSection("Args").Get<string[]>() ?? Array.Empty<string>();
        if (!enabled || string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("LSP server not configured");

        var psi = new ProcessStartInfo(path) { WorkingDirectory = workspaceRoot };
        psi.RedirectStandardInput = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }
}
