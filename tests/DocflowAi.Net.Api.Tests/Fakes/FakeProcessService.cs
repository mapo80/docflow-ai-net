using DocflowAi.Net.Api.JobQueue.Processing;

namespace DocflowAi.Net.Api.Tests.Fakes;

public class FakeProcessService : IProcessService
{
    public enum Mode
    {
        Success,
        Fail,
        Slow,
        Cancellable
    }

    public Mode CurrentMode { get; set; } = Mode.Success;
    public TimeSpan SlowDelay { get; set; } = TimeSpan.FromSeconds(5);

    private int _current;
    private int _maxConcurrent;
    public int MaxConcurrent => _maxConcurrent;

    public async Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
    {
        var now = Interlocked.Increment(ref _current);
        int snapshot;
        while (now > (snapshot = _maxConcurrent))
        {
            if (Interlocked.CompareExchange(ref _maxConcurrent, now, snapshot) == snapshot)
                break;
        }

        try
        {
            switch (CurrentMode)
            {
                case Mode.Success:
                    return new ProcessResult(true, "{\"ok\":true}", null);
                case Mode.Fail:
                    return new ProcessResult(false, null, "boom");
                case Mode.Slow:
                    await Task.Delay(SlowDelay, ct);
                    return new ProcessResult(true, "{}", null);
                case Mode.Cancellable:
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return new ProcessResult(true, "{}", null);
                default:
                    return new ProcessResult(true, "{}", null);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }
}

