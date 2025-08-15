using DocflowAi.Net.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace DocflowAi.Net.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _root;
    private readonly int _permit;
    private readonly int _windowSeconds;
    private readonly int _maxQueueLength;
    private readonly int _uploadLimitMb;
    private readonly int _workerCount;
    private readonly Dictionary<string,string?>? _extra;

    public string DataRootPath { get; private set; } = string.Empty;
    public string DbPath { get; private set; } = string.Empty;

    public TestWebAppFactory(
        string root,
        int permit = 1000,
        int windowSeconds = 60,
        int maxQueueLength = 3,
        int uploadLimitMb = 5,
        int workerCount = 1,
        Dictionary<string,string?>? extra = null)
    {
        _root = root;
        _permit = permit;
        _windowSeconds = windowSeconds;
        _maxQueueLength = maxQueueLength;
        _uploadLimitMb = uploadLimitMb;
        _workerCount = workerCount;
        _extra = extra;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
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
                ["JobQueue:RateLimit:General:PermitPerWindow"] = _permit.ToString(),
                ["JobQueue:RateLimit:General:WindowSeconds"] = _windowSeconds.ToString(),
                ["JobQueue:RateLimit:General:QueueLimit"] = "0",
                ["JobQueue:RateLimit:Submit:PermitPerWindow"] = _permit.ToString(),
                ["JobQueue:RateLimit:Submit:WindowSeconds"] = _windowSeconds.ToString(),
                ["JobQueue:RateLimit:Submit:QueueLimit"] = "0",
                ["Serilog:MinimumLevel:Default"] = "Information",
                ["JobQueue:EnableDashboard"] = "false",
                ["JobQueue:Queue:MaxQueueLength"] = _maxQueueLength.ToString(),
                ["JobQueue:Concurrency:HangfireWorkerCount"] = _workerCount.ToString(),
                ["JobQueue:UploadLimits:MaxRequestBodyMB"] = _uploadLimitMb.ToString(),
                ["Serilog:Using:0"] = "Serilog.Sinks.TestCorrelator",
                ["Serilog:WriteTo:0:Name"] = "TestCorrelator"
            };
            if (_extra != null)
                foreach (var kv in _extra) dict[kv.Key] = kv.Value;
            config.AddInMemoryCollection(dict);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.TestCorrelator()
                .CreateLogger();
        });
    }
}
