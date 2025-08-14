using DocflowAi.Net.Api.JobQueue.Processing;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace DocflowAi.Net.Api.Tests.Helpers;

public class DelegateProcessService : IProcessService
{
    private readonly Func<ProcessInput, CancellationToken, Task<ProcessResult>> _impl;

    public DelegateProcessService(Func<ProcessInput, CancellationToken, Task<ProcessResult>> impl)
    {
        _impl = impl;
    }

    public Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
        => _impl(input, ct);
}
