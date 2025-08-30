namespace DocflowAi.Net.Api.Rules.Data;

using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

public static class RuleFunctionSeeder
{
    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();
        if (db.RuleFunctions.Any(r => r.IsBuiltin)) return;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

        var builtinRules = new List<RuleFunction>
        {
            new RuleFunction
            {
                Name = "Builtins.Iban.NormalizeAndValidate",
                IsBuiltin = true,
                Description = "Normalize IBAN from field ibanRaw and validate checksum",
                Code = @"
if (has(""ibanRaw"")) {
    var raw = (string)get(""ibanRaw"")!;
    var norm = Iban.Normalize(raw);
    set(""iban"", norm, 0.98, ""builtin:iban"");
    assert(Iban.IsValid(norm), ""Invalid IBAN"");
}
",
                Enabled = true
            },
            new RuleFunction
            {
                Name = "Builtins.Total.FromNetTax",
                IsBuiltin = true,
                Description = "Compute total = net + tax if missing",
                Code = @"
if (missing(""total"") && has(""net"") && has(""tax"")) {
    var net = get<decimal>(""net"") ?? 0m;
    var tax = get<decimal>(""tax"") ?? 0m;
    set(""total"", Money.Round(net + tax), 0.97, ""builtin:total"");
}
",
                Enabled = true
            }
        };

        foreach (var r in builtinRules)
        {
            r.CodeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Code)));
        }

        db.RuleFunctions.AddRange(builtinRules);
        db.SaveChanges();
        logger.LogInformation("SeededRuleFunctions");
    }
}
