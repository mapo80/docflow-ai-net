using System.Net.Http.Headers;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VerifyXunit;

namespace DocflowAi.Net.Tests.Integration;

public class ProcessEndpointSnapshotTests : IClassFixture<ProcessEndpointSnapshotTests.WebAppFactory>
{
    private readonly HttpClient _client;
    public ProcessEndpointSnapshotTests(WebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Process_ReturnsExpectedJson()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(100, 100);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("hello", 12, new PdfPoint(10, 90), font);
        var pdf = builder.Build();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdf);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "test.pdf");
        content.Add(new StringContent("tpl"), "templateName");
        content.Add(new StringContent("prompt"), "prompt");
        content.Add(new StringContent("name"), "fields[0].fieldName");
        content.Add(new StringContent("string"), "fields[0].format");
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/process");
        req.Headers.Add("X-API-Key", "dev-secret-key-change-me");
        req.Content = content;
        var resp = await _client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        await Verify(json, "json");
    }

    public class WebAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILlamaExtractor, FakeLlama>();
            });
            return base.CreateHost(builder);
        }
    }

    private class FakeLlama : ILlamaExtractor
    {
        public Task<DocumentAnalysisResult> ExtractAsync(string markdown, string templateName, string prompt, IReadOnlyList<FieldSpec> fields, CancellationToken ct)
        {
            var extracted = new List<ExtractedField> { new("name", "John", 0.99) };
            return Task.FromResult(new DocumentAnalysisResult("doc", extracted, "en", null));
        }
    }
}
