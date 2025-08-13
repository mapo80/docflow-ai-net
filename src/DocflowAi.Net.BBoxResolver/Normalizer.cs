using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DocflowAi.Net.BBoxResolver;

internal static class Normalizer
{
    private static readonly Regex TokenRegex = new("[0-9a-z]+", RegexOptions.Compiled);
    private static readonly Regex AcronymDots = new("(?<=\\p{L})\\.(?=\\p{L})", RegexOptions.Compiled);

    public static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;
        var n = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        n = AcronymDots.Replace(n, string.Empty);
        n = string.Join(' ', n.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        if (double.TryParse(n, NumberStyles.Any, CultureInfo.GetCultureInfo("it-IT"), out var it))
            return it.ToString(CultureInfo.InvariantCulture);
        if (double.TryParse(n, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out var en))
            return en.ToString(CultureInfo.InvariantCulture);
        return n;
    }

    public static string[] Tokenize(string s)
    {
        var norm = Normalize(s);
        var matches = TokenRegex.Matches(norm);
        var tokens = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            tokens[i] = matches[i].Value;
        return tokens;
    }
}
