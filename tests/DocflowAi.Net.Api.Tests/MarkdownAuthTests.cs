using System.Linq;
using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","MarkdownEndpoints")]
public class MarkdownAuthTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public MarkdownAuthTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Endpoint_requires_authorization()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var data = factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoint = data.Endpoints.OfType<RouteEndpoint>()
            .First(e => e.RoutePattern.RawText?.StartsWith("/api/v1/markdown") == true);
        endpoint.Metadata.GetMetadata<IAuthorizeData>().Should().NotBeNull();
    }
}
