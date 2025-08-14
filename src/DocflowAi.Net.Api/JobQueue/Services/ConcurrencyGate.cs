using DocflowAi.Net.Api.JobQueue.Abstractions;
using DocflowAi.Net.Api.Options;
using Microsoft.Extensions.Options;
using System.Threading;

namespace DocflowAi.Net.Api.JobQueue.Services;

public class ConcurrencyGate : IConcurrencyGate
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _capacity;

    public ConcurrencyGate(IOptions<JobQueueOptions> options)
    {
        _capacity = options.Value.Concurrency.MaxParallelHeavyJobs;
        _semaphore = new SemaphoreSlim(_capacity);
    }

    public Task WaitAsync(CancellationToken ct) => _semaphore.WaitAsync(ct);

    public bool TryEnter() => _semaphore.Wait(0);

    public void Release() => _semaphore.Release();

    public int InUse => _capacity - _semaphore.CurrentCount;

    public int Capacity => _capacity;
}
