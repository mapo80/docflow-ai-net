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
    public async Task Convert_returns_markdown_text()
    {
        var file = new FormFile(new MemoryStream(new byte[] {1,2,3}), 0, 3, "file", "test.png");
        var result = await MarkdownEndpoints.ConvertFileAsync(file, new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var ms = new MemoryStream();
        ctx.Response.Body = ms;
        await result.ExecuteAsync(ctx);
        ms.Position = 0;
        var text = await new StreamReader(ms).ReadToEndAsync();
        Assert.Equal("FAKE", text);
    }

    [Fact]
    public async Task Missing_file_returns_bad_request()
    {
        var result = await MarkdownEndpoints.ConvertFileAsync(null, new FakeMarkdownConverter(), Microsoft.Extensions.Options.Options.Create(new MarkdownOptions()));
        var json = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(400, json.StatusCode);
    }
}
