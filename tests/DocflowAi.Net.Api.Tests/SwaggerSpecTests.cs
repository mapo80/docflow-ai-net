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
    public async Task Swagger_Exposes_JobEndpoints_And_NoProcess_WithExamples()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath);
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name)
            .Should().Contain("/api/v1/jobs");
        doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name)
            .Should().NotContain("/process");

        var post = doc.RootElement.GetProperty("paths").GetProperty("/api/v1/jobs").GetProperty("post");
        var paramNames = post.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString());
        paramNames.Should().NotContain("mode");
        paramNames.Should().Contain("Idempotency-Key");
        var responses = post.GetProperty("responses");
        responses.GetProperty("202").GetProperty("content").GetProperty("application/json").GetProperty("example").Should().NotBeNull();
        responses.GetProperty("429").GetProperty("headers").GetProperty("Retry-After").Should().NotBeNull();
        responses.GetProperty("429").GetProperty("content").GetProperty("application/json").GetProperty("examples").EnumerateObject().Select(o=>o.Name)
            .Should().Contain(new[]{"queue_full"});
        responses.GetProperty("413").GetProperty("content").GetProperty("application/json").GetProperty("example").Should().NotBeNull();
    }
}
