using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","LspEndpoints")]
public class LspProxyEndpointsTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public LspProxyEndpointsTests(TempDirFixture fx) => _fx = fx;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        return client;
    }

    [Fact]
    public async Task Workspace_sync_writes_file()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.PostAsJsonAsync("/api/v1/lsp/workspace/sync?workspaceId=ws", new { filePath = "a.csx", content = "x" });
        resp.EnsureSuccessStatusCode();
        var env = factory.Services.GetRequiredService<IWebHostEnvironment>();
        var path = Path.Combine(env.ContentRootPath, "workspace", "ws", "a.csx");
        File.ReadAllText(path).Should().Be("x");
    }

    [Fact]
    public async Task Csharp_not_websocket_returns_400()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = CreateClient(factory);
        var resp = await client.GetAsync("/api/v1/lsp/csharp");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Csharp_disabled_returns_503()
    {
        var extra = new Dictionary<string,string?> { ["Lsp:Enabled"] = "false", ["Lsp:ServerPath"] = "/usr/bin/cat" };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var wsClient = factory.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = req => req.Headers["X-API-Key"] = "dev-secret-key-change-me";
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await wsClient.ConnectAsync(new Uri("ws://localhost/api/v1/lsp/csharp"), CancellationToken.None));
    }

    [Fact]
    public async Task Csharp_proxies_messages()
    {
        var extra = new Dictionary<string,string?> { ["Lsp:Enabled"] = "true", ["Lsp:ServerPath"] = "/usr/bin/cat" };
        await using var factory = new TestWebAppFactory(_fx.RootPath, extra: extra);
        var wsClient = factory.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = req => req.Headers["X-API-Key"] = "dev-secret-key-change-me";
        using var ws = await wsClient.ConnectAsync(new Uri("ws://localhost/api/v1/lsp/csharp"), CancellationToken.None);
        var msg = Encoding.UTF8.GetBytes("ping");
        await ws.SendAsync(msg, WebSocketMessageType.Text, true, CancellationToken.None);
        var buffer = new byte[4];
        var res = await ws.ReceiveAsync(buffer, CancellationToken.None);
        Encoding.UTF8.GetString(buffer, 0, res.Count).Should().Be("ping");
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
    }
}
