using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DocflowAi.Net.Application.Markdown;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class MarkdownDoclingIntegrationTests
{
    [Fact]
    public async Task Markdown_endpoint_calls_docling_service()
    {
        await using var docling = await StartDoclingStubAsync();
        var url = docling.Urls.Single();
        using var factory = new TestWebAppFactory(Path.GetTempPath(), extra: new()
        {
            ["Markdown:DoclingServeUrl"] = url,
            ["Api:Keys:0"] = "test-key"
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-key");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] {1,2,3}), "file", "test.png");

        var resp = await client.PostAsync("/api/v1/markdown?language=eng", content);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MarkdownResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("FAKE_MD", result!.Markdown);
    }

    private static async Task<WebApplication> StartDoclingStubAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapPost("/v1/convert/file", async (HttpRequest req) =>
        {
            await req.ReadFormAsync();
            return Results.Json(new { document = new { md_content = "FAKE_MD" } });
        });
        await app.StartAsync();
        return app;
    }
}
