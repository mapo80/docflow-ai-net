using System.Text.RegularExpressions;

namespace DocflowRules.Sdk;

public sealed class ScriptGlobals
{
    public ExtractionContext Ctx { get; init; } = default!;
    public object? get(string key) => Ctx.Get(key);
    public T? get<T>(string key) => Ctx.Get<T>(key);
    public void set(string key, object? value, double conf = 0.99, string src = "rule") => Ctx.Upsert(key, value, conf, src);
    public bool has(string key) => Ctx.Has(key);
    public bool missing(string key) => !Ctx.Has(key);
    public void assert(bool cond, string? msg = null) { if (!cond) throw new RuleAssertionException(msg ?? "Assertion failed"); }

    public TextUtil Text { get; } = new();
    public MoneyUtil Money { get; } = new();
    public DateUtil Date { get; } = new();
    public IbanUtil Iban { get; } = new();
    public CfUtil Cf { get; } = new();
    public RegexUtil Rx { get; } = new();
}

public sealed class TextUtil
{
    public string Trim(string s) => s.Trim();
    public string Upper(string s) => s.ToUpperInvariant();
    public string Lower(string s) => s.ToLowerInvariant();
    public string Replace(string s, string oldValue, string newValue) => s.Replace(oldValue, newValue);
}

public sealed class MoneyUtil
{
    public decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

public sealed class DateUtil
{
    public DateOnly? Parse(string s)
    {
        if (DateOnly.TryParse(s, out var d)) return d;
        if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);
        return null;
    }
}

public sealed class IbanUtil
{
    public string Normalize(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    public bool IsValid(string iban)
    {
        iban = Normalize(iban);
        if (iban.Length < 15 || iban.Length > 34) return false;
        var rearranged = iban[4..] + iban[..4];
        var sb = new System.Text.StringBuilder();
        foreach (var ch in rearranged)
            sb.Append(char.IsLetter(ch) ? ((ch - 'A') + 10).ToString() : ch);
        int rem = 0;
        foreach (var c in sb.ToString())
            rem = (rem * 10 + (c - '0')) % 97;
        return rem == 1;
    }
}

public sealed class CfUtil
{
    public string Sex(string cf) => "M";
    public DateOnly? BirthDate(string cf) => null;
}

public sealed class RegexUtil
{
    public bool Match(string input, string pattern) => Regex.IsMatch(input, pattern);
    public string? Extract(string input, string pattern, int group = 1)
    {
        var m = Regex.Match(input, pattern);
        if (m.Success && m.Groups.Count > group) return m.Groups[group].Value;
        return null;
    }
}

public sealed class RuleAssertionException : Exception
{
    public RuleAssertionException(string message) : base(message) { }
}
