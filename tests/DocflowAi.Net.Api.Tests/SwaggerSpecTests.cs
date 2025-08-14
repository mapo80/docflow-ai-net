using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using DocflowAi.Net.Api.Tests.Fixtures;

namespace DocflowAi.Net.Api.Tests;

public class SwaggerSpecTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;

    public SwaggerSpecTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Swagger_Exposes_JobEndpoints_And_NoProcess()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name)
            .Should().Contain("/v1/jobs");
        doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name)
            .Should().NotContain("/process");
    }
}
