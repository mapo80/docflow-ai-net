using System.Collections.Generic;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class DefaultJobSeederTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;

    public DefaultJobSeederTests(TempDirFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seeds_Two_Default_Jobs()
    {
        var extra = new Dictionary<string, string?>
        {
            ["JobQueue:SeedDefaults"] = "true"
        };
        using var factory = new TestWebAppFactory(_fixture.RootPath, extra: extra);
        var client = factory.CreateClient();
        var resp = await client.GetFromJsonAsync<JobListResponse>("/api/v1/jobs");

        resp!.total.Should().Be(2);
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        resp.items.Select(j => j.id).Should().Contain(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    private record JobListResponse(int page, int pageSize, int total, List<JobItem> items);
    private record JobItem(Guid id, string status, string derivedStatus, int progress, DateTimeOffset createdAt, DateTimeOffset updatedAt);
}
