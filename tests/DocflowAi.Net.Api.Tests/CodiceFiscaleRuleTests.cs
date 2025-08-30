using System.Text.Json.Nodes;
using DocflowAi.Net.Api.Rules.Runtime;
using FluentAssertions;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class CodiceFiscaleRuleTests
{
    private const string Code = @"if (g.Has(""cf""))
{
    var value = g.Get(""cf"")?.ToString() ?? string.Empty;
    value = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    g.Set(""cf"", value);
}";

    [Fact]
    public async Task Normalizes_cf_and_is_idempotent()
    {
        var runner = new RoslynScriptRunner();
        var input = new JsonObject
        {
            ["fields"] = new JsonObject
            {
                ["cf"] = new JsonObject { ["value"] = " rss-mra85t10 a562 s " }
            }
        };

        var (_, after1, _, _, _) = await runner.RunAsync(Code, input, CancellationToken.None);
        after1["cf"]!.GetValue<string>().Should().Be("RSSMRA85T10A562S");

        var input2 = new JsonObject
        {
            ["fields"] = new JsonObject
            {
                ["cf"] = new JsonObject { ["value"] = after1["cf"]!.GetValue<string>() }
            }
        };

        var (_, after2, _, _, _) = await runner.RunAsync(Code, input2, CancellationToken.None);
        after2["cf"]!.GetValue<string>().Should().Be("RSSMRA85T10A562S");
    }
}
