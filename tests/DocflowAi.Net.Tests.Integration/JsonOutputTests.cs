using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.BBoxResolver;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace DocflowAi.Net.Tests.Integration;

public class JsonOutputTests : IClassFixture<JsonOutputTests.JsonWebAppFactory>
{
    private readonly HttpClient _client;
    public JsonOutputTests(JsonWebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Process_ReturnsValidJson()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
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
        var text = await resp.Content.ReadAsStringAsync();
        JsonDocument.Parse(text); // throws if invalid
        var trimmed = text.TrimStart();
        (trimmed.StartsWith("{") || trimmed.StartsWith("[")).Should().BeTrue();
    }

    public class JsonWebAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IProcessingOrchestrator, FakeOrchestrator>();
            });
            return base.CreateHost(builder);
        }
    }

    private class FakeOrchestrator : IProcessingOrchestrator
    {
        public Task<DocumentAnalysisResult> ProcessAsync(IFormFile file, string templateName, string prompt, IReadOnlyList<FieldSpec> fields, CancellationToken ct)
        {
            var result = new DocumentAnalysisResult("doc", new List<ExtractedField>(), "en", null);
            return Task.FromResult(result);
        }

        public Task<DocumentAnalysisResult> ProcessAsync(IFormFile file, string templateName, string prompt, IReadOnlyList<string> fieldNames, CancellationToken ct)
            => ProcessAsync(file, templateName, prompt, fieldNames.Select(n => new FieldSpec { Key = n }).ToList(), ct);
    }
}
