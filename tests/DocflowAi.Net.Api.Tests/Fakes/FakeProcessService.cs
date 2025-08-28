using DocflowAi.Net.Api.JobQueue.Processing;
using System;
using System.IO;

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

        await File.WriteAllTextAsync(input.MarkdownPath, "# md", ct);
        await File.WriteAllTextAsync(Path.ChangeExtension(input.MarkdownPath, ".json"), "{\"markdown\":\"md\"}", ct);
        await File.WriteAllTextAsync(input.PromptPath, "prompt", ct);
        var ts = DateTimeOffset.UtcNow;

        try
        {
            switch (CurrentMode)
            {
                case Mode.Success:
                    return new ProcessResult(true, "{\"ok\":true}", "# md", null, ts, ts);
                case Mode.Fail:
                    return new ProcessResult(false, string.Empty, "# md", "boom", ts, ts);
                case Mode.Slow:
                    await Task.Delay(SlowDelay, ct);
                    return new ProcessResult(true, "{}", "# md", null, ts, ts);
                case Mode.Cancellable:
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return new ProcessResult(true, "{}", "# md", null, ts, ts);
                default:
                    return new ProcessResult(true, "{}", "# md", null, ts, ts);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }
}

