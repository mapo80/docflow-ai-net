namespace DocflowAi.Net.Api.Rules.Data;

using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class RuleTestCaseSeeder
{
    public static void Build(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        db.Database.EnsureCreated();
        if (db.RuleTestCases.Any()) return;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

        var rule = db.RuleFunctions.First(r => r.Name == "Builtins.Iban.NormalizeAndValidate");
        db.RuleTestCases.Add(new RuleTestCase
        {
            RuleFunctionId = rule.Id,
            Name = "IBAN ok",
            InputJson = @"{ ""fields"": { ""ibanRaw"": { ""value"": ""it 60 x054 2811 1010 0000 0123 456"" } } }",
            ExpectJson = @"{ ""fields"": { ""iban"": { ""regex"": ""^[A-Z0-9]{15,34}$"" } } }"
        });
        db.SaveChanges();
        logger.LogInformation("SeededRuleTestCases");
    }
}
