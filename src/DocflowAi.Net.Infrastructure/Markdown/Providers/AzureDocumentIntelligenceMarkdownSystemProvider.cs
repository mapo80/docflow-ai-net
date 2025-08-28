using System;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Infrastructure.Markdown;

namespace DocflowAi.Net.Infrastructure.Markdown.Providers;

/// <summary>Markdown provider using Azure Document Intelligence.</summary>
public sealed class AzureDocumentIntelligenceMarkdownSystemProvider : IMarkdownSystemProvider
{
    public string Name => "azure-di";

    public IMarkdownConverter Create(string endpoint, string? apiKey)
        => new AzureDocumentIntelligenceMarkdownConverter(new Uri(endpoint), apiKey);
}
