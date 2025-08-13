namespace XFundEvalRunner.Models;

public sealed class Pointer
{
    public PointerMode Mode { get; }
    public string[]? WordIds { get; }
    public int? Start { get; }
    public int? End { get; }

    public Pointer(string[] wordIds)
    {
        Mode = PointerMode.WordIds;
        WordIds = wordIds;
    }

    public Pointer(int start, int end)
    {
        Mode = PointerMode.Offsets;
        Start = start;
        End = end;
    }
}
