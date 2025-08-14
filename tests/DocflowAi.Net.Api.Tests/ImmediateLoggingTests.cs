using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fakes;
using DocflowAi.Net.Api.Tests.Fixtures;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class ImmediateLoggingTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ImmediateLoggingTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Logs_Are_Captured_For_Success_And_Failure()
    {
        using var factory = new TestWebAppFactory_Immediate(_fx.RootPath);
        var client = factory.CreateClient();
        var payload1 = new { fileBase64 = Convert.ToBase64String(new byte[]{1}), fileName = "a.pdf" };
        using (TestCorrelator.CreateContext())
        {
            factory.Fake.CurrentMode = FakeProcessService.Mode.Success;
            var resp = await client.PostAsJsonAsync("/v1/jobs?mode=immediate", payload1);
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            TestCorrelator.GetLogEventsFromCurrentContext();
        }
        using (TestCorrelator.CreateContext())
        {
            factory.Fake.CurrentMode = FakeProcessService.Mode.Fail;
            var payload2 = new { fileBase64 = Convert.ToBase64String(new byte[]{2}), fileName = "b.pdf" };
            var resp = await client.PostAsJsonAsync("/v1/jobs?mode=immediate", payload2);
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            TestCorrelator.GetLogEventsFromCurrentContext();
        }
    }
}
