namespace DocflowAi.Net.Application.Abstractions;

/// <summary>Factory for markdown converters based on provider-specific configuration.</summary>
public interface IMarkdownSystemProvider
{
    /// <summary>The provider identifier.</summary>
    string Name { get; }

    /// <summary>Create a markdown converter for the given endpoint and optional API key.</summary>
    IMarkdownConverter Create(string endpoint, string? apiKey);
}
