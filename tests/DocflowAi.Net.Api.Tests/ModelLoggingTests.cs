using System.Net.Http.Json;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using FluentAssertions;
using Serilog.Sinks.TestCorrelator;
using Microsoft.Extensions.DependencyInjection;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Api.Tests;

public class ModelLoggingTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelLoggingTests(TempDirFixture fx) => _fx = fx;

    [Fact]
    public async Task Logs_Are_Emitted()
    {
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, FakeLlmModelService>()));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        using (TestCorrelator.CreateContext())
        {
            await client.PostAsJsonAsync("/api/v1/model/switch", new { hfKey = "k", modelRepo = "r", modelFile = "f", contextSize = 10 });
            await client.GetAsync("/api/v1/model/status");
            var logs = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelSwitchStarted"));
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelStatusFetched"));
        }
    }
}
