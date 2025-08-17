using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Storage.EF;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext db)
    {
        var exists = await db.RuleFunctions.AnyAsync(r => r.IsBuiltin);
        if (exists) return;

        var builtinRules = new List<RuleFunction>
        {
            new RuleFunction {
                Name = "Builtins.Iban.NormalizeAndValidate",
                IsBuiltin = true,
                Description = "Normalizza l'IBAN dal campo ibanRaw e valida checksum",
                Code = """
if (has("ibanRaw")) {
    var raw = (string)get("ibanRaw")!;
    var norm = Iban.Normalize(raw);
    set("iban", norm, 0.98, "builtin:iban");
    assert(Iban.IsValid(norm), "IBAN non valido");
}
""",
                Enabled = true
            },
            new RuleFunction {
                Name = "Builtins.Total.FromNetTax",
                IsBuiltin = true,
                Description = "Calcola total = net + tax se mancante",
                Code = """
if (missing("total") && has("net") && has("tax")) {
    var net = get<decimal>("net") ?? 0m;
    var tax = get<decimal>("tax") ?? 0m;
    set("total", Money.Round(net + tax), 0.97, "builtin:total");
}
""",
                Enabled = true
            }
        };

        foreach (var r in builtinRules)
        {
            r.CodeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Code)));
        }

        db.RuleFunctions.AddRange(builtinRules);
        await db.SaveChangesAsync();

        var ibanRule = await db.RuleFunctions.FirstAsync(r => r.Name == "Builtins.Iban.NormalizeAndValidate");
        db.RuleTestCases.Add(new RuleTestCase
        {
            RuleFunctionId = ibanRule.Id,
            Name = "IBAN ok",
            InputJson = """
{ "fields": { "ibanRaw": { "value": "it 60 x054 2811 1010 0000 0123 456" } } }
""",
            ExpectJson = """
{ "fields": { "iban": { "regex": "^[A-Z0-9]{15,34}$" } } }
"""
        });

        await db.SaveChangesAsync();
    }
}
