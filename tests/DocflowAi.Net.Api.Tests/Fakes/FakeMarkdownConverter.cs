using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;

namespace DocflowAi.Net.Api.Tests.Fakes;

public sealed class FakeMarkdownConverter : IMarkdownConverter
{
    public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default) =>
        Task.FromResult(new MarkdownResult("FAKE", Array.Empty<PageInfo>(), Array.Empty<Box>()));

    public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default) =>
        Task.FromResult(new MarkdownResult("FAKE", Array.Empty<PageInfo>(), Array.Empty<Box>()));
}
