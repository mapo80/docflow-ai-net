using System.Net.Http;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Infrastructure.Markdown;
using DocflowAi.Net.Infrastructure.Markdown.DoclingServe;

namespace DocflowAi.Net.Infrastructure.Markdown.Providers;

/// <summary>Markdown provider using Docling Serve.</summary>
public sealed class DoclingMarkdownSystemProvider : IMarkdownSystemProvider
{
    private readonly IHttpClientFactory _httpFactory;

    public DoclingMarkdownSystemProvider(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public string Name => "docling";

    public IMarkdownConverter Create(string endpoint, string? apiKey)
    {
        var client = new DoclingServeClient(endpoint, _httpFactory.CreateClient());
        return new MarkdownNetConverter(client);
    }
}
