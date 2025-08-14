using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using LiteDB;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace DocflowAi.Net.Api.Health;

public class JobQueueReadyHealthCheck : IHealthCheck
{
    private readonly IOptions<JobQueueOptions> _options;
    private readonly IJobStore _store;
    private readonly ILogger<JobQueueReadyHealthCheck> _logger;

    public JobQueueReadyHealthCheck(IOptions<JobQueueOptions> options, IJobStore store, ILogger<JobQueueReadyHealthCheck> logger)
    {
        _options = options;
        _store = store;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();
        try
        {
            var opts = _options.Value;
            if (!Directory.Exists(opts.DataRoot))
            {
                reasons.Add("data_root_not_writable");
            }
            else
            {
                var probe = Path.Combine(opts.DataRoot, ".__probe");
                try
                {
                    File.WriteAllText(probe, "ok");
                    File.Delete(probe);
                }
                catch
                {
                    reasons.Add("data_root_not_writable");
                }
            }
            try
            {
                using var db = new LiteDatabase(opts.LiteDb.Path);
            }
            catch
            {
                reasons.Add("litedb_unavailable");
            }
            var pending = 0;
            try { pending = _store.CountPending(); }
            catch { reasons.Add("litedb_unavailable"); }
            if (reasons.Count == 0 && pending >= opts.Queue.MaxQueueLength * 2)
                reasons.Add("backpressure");
        }
        catch (Exception ex)
        {
            reasons.Add(ex.GetType().Name);
        }
        if (reasons.Count > 0)
        {
            _logger.LogWarning("HealthReadyFailed {Reasons}", string.Join(',', reasons));
            var data = new Dictionary<string, object> { ["reasons"] = reasons };
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "unhealthy", data: data));
        }
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
