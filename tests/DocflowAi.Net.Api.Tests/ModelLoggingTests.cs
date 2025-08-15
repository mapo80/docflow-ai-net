using System.Net.Http.Json;
using System.Net;
using DocflowAi.Net.Api.Tests.Fixtures;
using DocflowAi.Net.Api.Tests.Fakes;
using Serilog;
using Serilog.Sinks.TestCorrelator;
using Microsoft.Extensions.DependencyInjection;
using DocflowAi.Net.Application.Abstractions;

namespace DocflowAi.Net.Api.Tests;

[Trait("Category","ModelEndpoints")]
public class ModelLoggingTests : IClassFixture<TempDirFixture>
{
    private readonly TempDirFixture _fx;
    public ModelLoggingTests(TempDirFixture fx) => _fx = fx;

    [Fact(Skip="Serilog capture not configured")]
    public async Task Logs_Structured_On_Success()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.TestCorrelator()
            .CreateLogger();
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService, ConfigurableFakeLlmModelService>()));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        using (TestCorrelator.CreateContext())
        {
            await client.PostAsJsonAsync("/api/v1/model/download", new { hfKey = "k", modelRepo = "r", modelFile = "f" });
            await client.GetAsync("/api/v1/model/status");
            var logs = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelDownloadStarted"));
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelStatusFetched"));
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelDownloadCompleted"));
            logs.Should().NotContain(e => e.RenderMessage(null).Contains("k"));
        }
    }

    [Fact(Skip="Serilog capture not configured")]
    public async Task Logs_On_Failure()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.TestCorrelator()
            .CreateLogger();
        var fake = new ConfigurableFakeLlmModelService();
        fake.FailWith(new FileNotFoundException("missing"));
        await using var factory = new TestWebAppFactory(_fx.RootPath)
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILlmModelService>(fake)));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-secret-key-change-me");
        using (TestCorrelator.CreateContext())
        {
            var resp = await client.PostAsJsonAsync("/api/v1/model/download", new { hfKey = "k", modelRepo = "r", modelFile = "f" });
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var logs = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            logs.Should().Contain(e => e.MessageTemplate.Text.StartsWith("ModelDownloadFailed"));
        }
    }
}
