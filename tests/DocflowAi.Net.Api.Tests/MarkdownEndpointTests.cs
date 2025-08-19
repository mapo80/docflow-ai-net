using System;
using DocflowAi.Net.Api.Markdown.Endpoints;
using DocflowAi.Net.Api.Contracts;
using DocflowAi.Net.Api.Tests.Fakes;
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
        var result = await MarkdownEndpoints.ConvertFileAsync(file, new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
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
        var result = await MarkdownEndpoints.ConvertFileAsync(null, new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }

    [Theory]
    [InlineData("unsupported_format", 400)]
    [InlineData("conversion_failed", 422)]
    public async Task Conversion_errors_are_translated(string code, int status)
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, new ThrowingMarkdownConverter(code), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(status, json.StatusCode);
        Assert.Equal(code, json.Value.Error);
    }

    [Fact]
    public async Task Native_library_failure_returns_internal_error()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1}), 0, 1, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, new ThrowingMarkdownConverter(new DllNotFoundException("liblept.so.5")), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(500, json.StatusCode);
        Assert.Equal("native_library_missing", json.Value.Error);
    }
}
