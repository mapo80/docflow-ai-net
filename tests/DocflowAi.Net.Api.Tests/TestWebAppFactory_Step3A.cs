using DocflowAi.Net.Api;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class TestWebAppFactory_Step3A : WebApplicationFactory<Program>
{
    private readonly string _root;
    private readonly int _timeoutSeconds;

    public FakeProcessService Fake { get; } = new();
    public string DataRootPath { get; private set; } = string.Empty;
    public string LiteDbPath { get; private set; } = string.Empty;

    public TestWebAppFactory_Step3A(string root, int timeoutSeconds = 2)
    {
        _root = root;
        _timeoutSeconds = timeoutSeconds;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var guid = Guid.NewGuid().ToString();
            var basePath = Path.Combine(_root, guid);
            DataRootPath = Path.Combine(basePath, "data", "jobs");
            LiteDbPath = Path.Combine(basePath, "data", "app.db");
            Directory.CreateDirectory(DataRootPath);
            var dict = new Dictionary<string, string?>
            {
                ["JobQueue:DataRoot"] = DataRootPath,
                ["JobQueue:LiteDb:Path"] = LiteDbPath,
                ["JobQueue:Timeouts:JobTimeoutSeconds"] = _timeoutSeconds.ToString(),
                ["JobQueue:Concurrency:HangfireWorkerCount"] = "1",
                ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                ["JobQueue:EnableDashboard"] = "false"
            };
            config.AddInMemoryCollection(dict);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.TestCorrelator()
                .CreateLogger();
        });
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IProcessService>(Fake);
        });
    }
}
