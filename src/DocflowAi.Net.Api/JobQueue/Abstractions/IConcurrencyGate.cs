using System.Threading;
using System.Threading.Tasks;

namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IConcurrencyGate
{
    Task WaitAsync(CancellationToken ct);
    bool TryEnter();
    void Release();
    int InUse { get; }
    int Capacity { get; }
}
