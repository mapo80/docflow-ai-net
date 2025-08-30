using System.Diagnostics;
using System.Net.WebSockets;
using DocflowAi.Net.Api.Rules.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocflowAi.Net.Api.Rules.Endpoints;

public static class LspProxyEndpoints
{
    public static IEndpointRouteBuilder MapLspProxyEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/lsp")
            .WithTags("Lsp")
            .RequireAuthorization();

        group.MapPost("/workspace/sync", async ([FromQuery] string workspaceId, LspSyncReq req, LspProxyService svc, CancellationToken ct) =>
        {
            await svc.SyncAsync(workspaceId, req.FilePath, req.Content, ct);
            return Results.Ok(new { ok = true });
        });

        group.MapGet("/csharp", async (HttpContext ctx, [FromQuery] string? workspaceId, LspProxyService svc, CancellationToken ct) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
                return Results.StatusCode(400);

            try
            {
                var workspace = svc.EnsureWorkspace(workspaceId);
                var psi = svc.BuildServerProcess(workspace);
                using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start LSP server");

                var toServer = Task.Run(async () =>
                {
                    var buffer = new byte[8192];
                    while (ws.State == WebSocketState.Open)
                    {
                        var res = await ws.ReceiveAsync(buffer, ct);
                        if (res.MessageType == WebSocketMessageType.Close) break;
                        if (res.Count > 0)
                        {
                            await proc.StandardInput.BaseStream.WriteAsync(buffer.AsMemory(0, res.Count), ct);
                            await proc.StandardInput.BaseStream.FlushAsync(ct);
                        }
                    }
                    try { proc.StandardInput.Close(); } catch {}
                });

                var fromServer = Task.Run(async () =>
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = await proc.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(buffer, 0, read), WebSocketMessageType.Text, true, ct);
                    }
                });

                await Task.WhenAny(toServer, fromServer);
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct); } catch {}
                if (!proc.HasExited) try { proc.Kill(entireProcessTree: true); } catch {}
                return Results.Empty;
            }
            catch (InvalidOperationException)
            {
                return Results.StatusCode(503);
            }
        });

        return builder;
    }

    public record LspSyncReq(string? FilePath, string? Content);
}
