using DocflowAi.Net.Api;
using DocflowAi.Net.Api.JobQueue.Processing;
using DocflowAi.Net.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DocflowAi.Net.Api.Tests;

public class TestWebAppFactory_Step3B : WebApplicationFactory<Program>
{
    private readonly string _root;
    private readonly int _maxParallel;
    private AsyncServiceScope? _scope;

    public FakeProcessService Fake { get; } = new();
    public string DataRootPath { get; private set; } = string.Empty;
    public string DbPath { get; private set; } = string.Empty;

    public TestWebAppFactory_Step3B(string root, int maxParallel = 1)
    {
        _root = root;
        _maxParallel = maxParallel;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var guid = Guid.NewGuid().ToString();
            var basePath = Path.Combine(_root, guid);
            DataRootPath = Path.Combine(basePath, "data", "jobs");
            DbPath = Path.Combine(basePath, "data", "app.db");
            Directory.CreateDirectory(DataRootPath);
            var dict = new Dictionary<string, string?>
            {
                ["JobQueue:DataRoot"] = DataRootPath,
                ["JobQueue:Database:Provider"] = "sqlite",
                ["JobQueue:Database:ConnectionString"] = $"Data Source={DbPath}",
                ["JobQueue:Queue:LeaseWindowSeconds"] = "2",
                ["JobQueue:Queue:MaxAttempts"] = "2",
                ["JobQueue:Concurrency:MaxParallelHeavyJobs"] = _maxParallel.ToString(),
                ["JobQueue:Concurrency:HangfireWorkerCount"] = "1",
                ["JobQueue:Timeouts:JobTimeoutSeconds"] = "3",
                ["Serilog:Using:0"] = "Serilog.Sinks.TestCorrelator",
                ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                ["JobQueue:EnableDashboard"] = "false"
            };
            config.AddInMemoryCollection(dict);
        });
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IProcessService>(Fake);
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().Enrich.FromLogContext().WriteTo.TestCorrelator().CreateLogger();
        });
    }

    public T GetService<T>() where T : notnull
    {
        _scope ??= Services.CreateAsyncScope();
        return _scope.Value.ServiceProvider.GetRequiredService<T>();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_scope.HasValue)
            await _scope.Value.DisposeAsync();
        await base.DisposeAsync();
    }
}
