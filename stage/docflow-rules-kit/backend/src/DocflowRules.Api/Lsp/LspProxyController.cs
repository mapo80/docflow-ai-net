using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api.Lsp;


    static string EnsureWorkspace(string root, string? workspaceId, IWebHostEnvironment env)
    {
        var dir = Path.Combine(root, workspaceId ?? "default");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "lib"));
        // Copy SDK dll if available
        var sdkDll = Path.Combine(AppContext.BaseDirectory, "DocflowRules.Sdk.dll");
        var targetDll = Path.Combine(dir, "lib", "DocflowRules.Sdk.dll");
        if (System.IO.File.Exists(sdkDll) && !System.IO.File.Exists(targetDll))
            System.IO.File.Copy(sdkDll, targetDll, overwrite: true);

        // Create csproj referencing the SDK dll
        var csproj = Path.Combine(dir, "workspace.csproj");
        if (!System.IO.File.Exists(csproj))
        {
            System.IO.File.WriteAllText(csproj, """
<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=\"DocflowRules.Sdk\">
      <HintPath>lib/DocflowRules.Sdk.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
""");
        }
        return dir;
    }

    [HttpPost("/lsp/workspace/sync")]
    public async Task<IActionResult> WorkspaceSync([FromQuery] string workspaceId, [FromBody] SyncReq body)
    {
        var web = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var workspaceRoot = Path.Combine(AppContext.BaseDirectory, "workspace");
        var web = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        workspaceRoot = EnsureWorkspace(workspaceRoot, workspaceId, web);
        var dir = EnsureWorkspace(workspaceRoot, workspaceId, web);
        var file = Path.Combine(dir, body.FilePath ?? "user.csx");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await System.IO.File.WriteAllTextAsync(file, body.Content ?? string.Empty);
        return Ok(new { ok = true });
    }

    public record SyncReq(string? FilePath, string? Content);


[ApiController]
public class LspProxyController : ControllerBase
{
    [Route("/lsp/csharp")]
    public async Task Get([FromQuery] string? workspaceId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Lsp");
        var enabled = config.GetValue("Enabled", false);
        var serverPath = config.GetValue<string?>("ServerPath");
        var args = config.GetSection("Args").Get<string[]>() ?? Array.Empty<string>();

        if (!enabled || string.IsNullOrWhiteSpace(serverPath))
        {
            HttpContext.Response.StatusCode = 503;
            return;
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var workspaceRoot = Path.Combine(AppContext.BaseDirectory, "workspace");
        var web = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        workspaceRoot = EnsureWorkspace(workspaceRoot, workspaceId, web);
        Directory.CreateDirectory(workspaceRoot);
        if (!string.IsNullOrWhiteSpace(workspaceId))
            workspaceRoot = Path.Combine(workspaceRoot, workspaceId);
        Directory.CreateDirectory(workspaceRoot);
        var omniCfg = Path.Combine(workspaceRoot, "omnisharp.json");
        if (!System.IO.File.Exists(omniCfg))
        {
            await System.IO.File.WriteAllTextAsync(omniCfg, "{\n  \"RoslynExtensionsOptions\": { \"EnableAnalyzersSupport\": true, \"EnableImportCompletion\": true }\n}\n");
        }
    
        var psi = new ProcessStartInfo(serverPath) { WorkingDirectory = workspaceRoot }
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start LSP server");

        var toServer = Task.Run(async () =>
        {
            var buffer = new byte[8192];
            while (ws.State == WebSocketState.Open)
            {
                var res = await ws.ReceiveAsync(buffer, HttpContext.RequestAborted);
                if (res.MessageType == WebSocketMessageType.Close) break;
                if (res.Count > 0)
                {
                    await proc.StandardInput.BaseStream.WriteAsync(buffer.AsMemory(0, res.Count), HttpContext.RequestAborted);
                    await proc.StandardInput.BaseStream.FlushAsync(HttpContext.RequestAborted);
                }
            }
            try { proc.StandardInput.Close(); } catch {}
        });

        var fromServer = Task.Run(async () =>
        {
            var buffer = new byte[8192];
            int read;
            while ((read = await proc.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, HttpContext.RequestAborted)) > 0)
            {
                await ws.SendAsync(new ArraySegment<byte>(buffer, 0, read), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: HttpContext.RequestAborted);
            }
        });

        await Task.WhenAny(toServer, fromServer);
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", HttpContext.RequestAborted); } catch {}
        if (!proc.HasExited) try { proc.Kill(entireProcessTree: true); } catch {}
    }
}
