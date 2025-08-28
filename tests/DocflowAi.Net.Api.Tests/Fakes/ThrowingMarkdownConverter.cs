using System;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using DocflowAi.Net.Infrastructure.Markdown;

namespace DocflowAi.Net.Api.Tests.Fakes;

public sealed class ThrowingMarkdownConverter : IMarkdownConverter
{
    private readonly Exception _exception;

    public ThrowingMarkdownConverter(string code)
        : this(new MarkdownConversionException(code, "fail"))
    {
    }

    public ThrowingMarkdownConverter(Exception exception)
    {
        _exception = exception;
    }

    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => Task.FromException<MarkdownResult>(_exception);

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, Guid? systemId = null, CancellationToken ct = default)
        => Task.FromException<MarkdownResult>(_exception);
}
