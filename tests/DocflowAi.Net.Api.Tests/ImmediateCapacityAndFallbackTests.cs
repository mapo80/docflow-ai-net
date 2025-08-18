using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DocflowAi.Net.Api.Tests.Helpers;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.JobQueue.Data;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateCapacityAndFallbackTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateCapacityAndFallbackTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Immediate_Capacity429_WhenMaxParallelReached_AndNoFallback()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath, fallback:false);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Slow;
        var client = factory.CreateClient();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf", model = "m", templateToken = "t" };
        var first = client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", payload); // running
        await Task.Delay(200); // ensure started
        var secondPayload = new { fileBase64 = Convert.ToBase64String(new byte[]{2}), fileName = "b.pdf", model = "m", templateToken = "t" };
        var second = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", secondPayload);
        second.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
        second.Headers.Should().ContainKey("Retry-After");
        (await second.Content.ReadAsStringAsync()).Should().Contain("immediate_capacity");
        await first; // cleanup
    }

    [Fact]
    public async Task Immediate_FallbackToQueue202_WhenCapacityReached()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath, fallback:true);
        factory.Fake.CurrentMode = FakeProcessService.Mode.Slow;
        var client = factory.CreateClient();
        var payload = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf", model = "m", templateToken = "t" };
        var first = client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", payload);
        for (var i = 0; i < 50 && factory.Fake.MaxConcurrent == 0; i++)
            await Task.Delay(100);
        var secondPayload = new { fileBase64 = Convert.ToBase64String(new byte[]{2}), fileName = "b.pdf", model = "m", templateToken = "t" };
        var second = await client.PostAsJsonAsync("/api/v1/jobs?mode=immediate", secondPayload);
        second.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("job_id").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        DbTestHelper.GetJob(db, id)!.Status.Should().Be("Queued");
        await first;
    }
}
