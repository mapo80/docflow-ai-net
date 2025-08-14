using System;
using System.Threading;
using System.Threading.Tasks;

namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IJobRunner
{
    Task Run(Guid jobId, CancellationToken ct, bool acquireGate = true, int? overrideTimeoutSeconds = null);
}
