using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocflowAi.Net.Api.Markdown.Endpoints;
using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Application.Markdown;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class MarkdownEndpointTests
{
    [Fact]
    public async Task Convert_returns_markdown_json()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1,2,3}), 0, 3, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "eng", "tesseract", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var ms = new MemoryStream();
        ctx.Response.Body = ms;
        await result.ExecuteAsync(ctx);
        ms.Position = 0;
        var json = await new StreamReader(ms).ReadToEndAsync();
        var obj = System.Text.Json.JsonSerializer.Deserialize<MarkdownResult>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("FAKE", obj!.Markdown);
    }

    [Fact]
    public async Task Missing_file_returns_bad_request()
    {
        var result = await MarkdownEndpoints.ConvertFileAsync(null, "eng", "tesseract", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    [Theory]
    [InlineData("unsupported_format", 400)]
    [InlineData("conversion_failed", 422)]
    public async Task Conversion_errors_are_translated(string code, int status)
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "eng", "tesseract", new ThrowingMarkdownConverter(code), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(status, json.StatusCode);
        Assert.Equal(code, json.Value.Error);
    }

    [Fact]
    public async Task Native_library_failure_returns_internal_error()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "eng", "tesseract", new ThrowingMarkdownConverter(new DllNotFoundException("liblept.so.5")), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(500, json.StatusCode);
        Assert.Equal("native_library_missing", json.Value.Error);
    }

    [Fact]
    public async Task Language_parameter_is_passed_to_converter()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1,2,3}), 0, 3, "file", "test.png");
        var conv = new RecordingMarkdownConverter();
        await MarkdownEndpoints.ConvertFileAsync(file, "eng", "rapidocr", conv, Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        Assert.Equal("eng", conv.LastOptions.OcrLanguage);
        Assert.Equal(OcrEngine.RapidOcr, conv.LastOptions.Engine);
    }

    [Fact]
    public async Task Missing_language_returns_bad_request()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, null, "tesseract", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    [Fact]
    public async Task Invalid_language_returns_bad_request()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "fra", "tesseract", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    [Fact]
    public async Task Latin_with_tesseract_returns_error()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "lat", "tesseract", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(422, json.StatusCode);
        Assert.Equal("unsupported_language", json.Value.Error);
    }

    [Fact]
    public async Task Missing_engine_returns_bad_request()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "eng", null, new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    [Fact]
    public async Task Invalid_engine_returns_bad_request()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, "eng", "foo", new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    private sealed class RecordingMarkdownConverter : IMarkdownConverter
    {
        public MarkdownOptions? LastOptions { get; private set; }

        public Task<MarkdownResult> ConvertPdfAsync(Stream pdf, MarkdownOptions opts, CancellationToken ct = default)
        {
            LastOptions = opts;
            return Task.FromResult(new MarkdownResult(string.Empty, Array.Empty<PageInfo>(), Array.Empty<Box>()));
        }

        public Task<MarkdownResult> ConvertImageAsync(Stream image, MarkdownOptions opts, CancellationToken ct = default)
        {
            LastOptions = opts;
            return Task.FromResult(new MarkdownResult(string.Empty, Array.Empty<PageInfo>(), Array.Empty<Box>()));
        }
    }
}
