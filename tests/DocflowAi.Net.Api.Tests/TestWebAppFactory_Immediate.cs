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

public class TestWebAppFactory_Immediate : WebApplicationFactory<Program>
{
    private readonly string _root;
    private readonly bool _fallback;
    private readonly int _timeout;
    private readonly int _maxParallel;
    private readonly int _maxQueueLength;
    private AsyncServiceScope? _scope;

    public FakeProcessService Fake { get; } = new();
    public string DataRootPath { get; private set; } = string.Empty;
    public string DbPath { get; private set; } = string.Empty;

    public TestWebAppFactory_Immediate(string root, bool fallback = false, int timeoutSeconds = 3, int maxParallel = 1, int maxQueueLength = 100)
    {
        _root = root;
        _fallback = fallback;
        _timeout = timeoutSeconds;
        _maxParallel = maxParallel;
        _maxQueueLength = maxQueueLength;
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
                ["JobQueue:Immediate:Enabled"] = "true",
                ["JobQueue:Immediate:FallbackToQueue"] = _fallback.ToString().ToLowerInvariant(),
                ["JobQueue:Immediate:TimeoutSeconds"] = _timeout.ToString(),
                ["JobQueue:Immediate:MaxParallel"] = _maxParallel.ToString(),
                ["JobQueue:Queue:MaxQueueLength"] = _maxQueueLength.ToString(),
                ["JobQueue:Concurrency:HangfireWorkerCount"] = "1",
                ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                ["JobQueue:EnableDashboard"] = "false",
                ["JobQueue:SeedDefaults"] = "false"
            };
            config.AddInMemoryCollection(dict);
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().Enrich.FromLogContext().WriteTo.TestCorrelator().CreateLogger();
        });
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IProcessService>(Fake);
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
