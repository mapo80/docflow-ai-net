using System.Net;
using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.JobQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;

namespace DocflowAi.Net.Api.Tests;

public class JobsListEndpointTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fixture;
    private readonly DateTimeOffset _baseTime = new(2024,1,1,0,0,0,TimeSpan.Zero);

    public JobsListEndpointTests(TempDirFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetJobs_Empty()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/v1/jobs");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await resp.Content.ReadFromJsonAsync<JobListResponse>();
        data!.page.Should().Be(1);
        data.pageSize.Should().Be(20);
        data.total.Should().Be(0);
        data.items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJobs_PagingAndOrdering()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var db = factory.Services.GetRequiredService<LiteDatabase>();
        var jobs = Enumerable.Range(0,35)
            .Select(i => LiteDbTestHelper.CreateJob(Guid.NewGuid(), "Queued", _baseTime.AddMinutes(i)))
            .ToList();
        LiteDbTestHelper.SeedJobs(db, jobs);

        var r1 = await client.GetFromJsonAsync<JobListResponse>("/v1/jobs?page=1&pageSize=10");
        r1!.total.Should().Be(35);
        r1.items.Should().HaveCount(10);
        r1.items[0].id.Should().Be(jobs[34].Id);
        r1.items[9].id.Should().Be(jobs[25].Id);

        var r2 = await client.GetFromJsonAsync<JobListResponse>("/v1/jobs?page=2&pageSize=10");
        r2!.items[0].id.Should().Be(jobs[24].Id);
        r2.items[9].id.Should().Be(jobs[15].Id);

        var r4 = await client.GetFromJsonAsync<JobListResponse>("/v1/jobs?page=4&pageSize=10");
        r4!.items.Should().HaveCount(5);
        r4.items[0].id.Should().Be(jobs[4].Id);
        r4.items[4].id.Should().Be(jobs[0].Id);
    }

    [Fact]
    public async Task GetJobs_PageValidationAndClamp()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var bad = await client.GetAsync("/v1/jobs?page=0");
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await bad.Content.ReadFromJsonAsync<ErrorResponse>();
        err!.error.Should().Be("bad_request");

        var db = factory.Services.GetRequiredService<LiteDatabase>();
        var jobs = Enumerable.Range(0,150)
            .Select(i => LiteDbTestHelper.CreateJob(Guid.NewGuid(), "Queued", _baseTime.AddMinutes(i)))
            .ToList();
        LiteDbTestHelper.SeedJobs(db, jobs);
        var resp = await client.GetFromJsonAsync<JobListResponse>("/v1/jobs?page=1&pageSize=500");
        resp!.pageSize.Should().Be(100);
        resp.items.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetJobs_DerivedStatusMapping()
    {
        using var factory = new TestWebAppFactory(_fixture.RootPath);
        var client = factory.CreateClient();
        var db = factory.Services.GetRequiredService<LiteDatabase>();
        var statuses = new[] { "Queued", "Running", "Succeeded", "Failed", "Cancelled" };
        var jobs = statuses.Select((s,i) => LiteDbTestHelper.CreateJob(Guid.NewGuid(), s, _baseTime.AddMinutes(i))).ToList();
        LiteDbTestHelper.SeedJobs(db, jobs);
        var resp = await client.GetFromJsonAsync<JobListResponse>("/v1/jobs?page=1&pageSize=10");
        resp!.items.Should().HaveCount(5);
        var map = new Dictionary<string,string>{
            ["Queued"]="Pending",
            ["Running"]="Processing",
            ["Succeeded"]="Completed",
            ["Failed"]="Failed",
            ["Cancelled"]="Cancelled"
        };
        foreach (var item in resp.items)
        {
            map[item.status].Should().Be(item.derivedStatus);
        }
    }

    private record JobListResponse(int page, int pageSize, int total, List<JobItem> items);
    private record JobItem(Guid id, string status, string derivedStatus, int progress, DateTimeOffset createdAt, DateTimeOffset updatedAt);
    private record ErrorResponse(string error, string? message);
}
