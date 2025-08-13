using System.Buffers;
using System.Linq;

namespace DocflowAi.Net.BBoxResolver;

internal static class Distance
{
    public static int ClassicLevenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;
        var prev = ArrayPool<int>.Shared.Rent(b.Length + 1);
        var curr = ArrayPool<int>.Shared.Rent(b.Length + 1);
        try
        {
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[b.Length];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(prev);
            ArrayPool<int>.Shared.Return(curr);
        }
    }

    public static int BitParallelMyers(string a, string b)
    {
        if (a.Length > 64 || b.Length > 64)
            return ClassicLevenshtein(a, b);
        if (a.Any(c => c >= 128) || b.Any(c => c >= 128))
            return ClassicLevenshtein(a, b);
        ulong[] peq = new ulong[128];
        for (int i = 0; i < a.Length; i++)
            peq[a[i]] |= 1UL << i;
        ulong pv = ~0UL, mv = 0, eq, xv, xh, ph, mh;
        int dist = a.Length;
        for (int i = 0; i < b.Length; i++)
        {
            eq = peq[b[i]];
            xv = eq | mv;
            xh = (((eq & pv) + pv) ^ pv) | eq;
            ph = mv | ~(xh | pv);
            mh = pv & xh;
            if ((ph & (1UL << (a.Length - 1))) != 0) dist++;
            else if ((mh & (1UL << (a.Length - 1))) != 0) dist--;
            ph = (ph << 1) | 1;
            mh <<= 1;
            pv = mh | ~(xv | ph);
            mv = ph & xv;
        }
        return dist;
    }
}
