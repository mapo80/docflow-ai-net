using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocflowAi.Net.Api.MarkdownSystem.Abstractions;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;

namespace DocflowAi.Net.Api.Markdown.Services;

/// <summary>Converter delegating to the configured markdown system.</summary>
public sealed class MarkdownSystemConverter : IMarkdownConverter
{
    private readonly IMarkdownSystemRepository _repo;
    private readonly IReadOnlyDictionary<string, IMarkdownSystemProvider> _providers;
    private readonly ISecretProtector _protector;

    public MarkdownSystemConverter(
        IMarkdownSystemRepository repo,
        IEnumerable<IMarkdownSystemProvider> providers,
        ISecretProtector protector)
    {
        _repo = repo;
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _protector = protector;
    }

    private IMarkdownConverter Resolve(Guid? systemId)
    {
        var system = systemId.HasValue
            ? _repo.GetById(systemId.Value)
            : _repo.GetAll().FirstOrDefault();
        if (system == null) throw new InvalidOperationException("no markdown system configured");
        if (!_providers.TryGetValue(system.Provider, out var provider))
            throw new InvalidOperationException("unknown markdown system provider");
        var apiKey = system.ApiKeyEncrypted != null ? _protector.Unprotect(system.ApiKeyEncrypted) : null;
        return provider.Create(system.Endpoint, apiKey);
    }

    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => Resolve(systemId).ConvertPdfAsync(pdf, opts, systemId: null, ct);

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => Resolve(systemId).ConvertImageAsync(image, opts, systemId: null, ct);
}
