using System.Net;
using FluentAssertions;
using DocflowAi.Net.Api.Tests.Fixtures;

namespace DocflowAi.Net.Api.Tests;

public class ProcessEndpointRemovalTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ProcessEndpointRemovalTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task ProcessEndpoint_Removed_Or_Obsoleted()
    {
        using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/v1/process");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
