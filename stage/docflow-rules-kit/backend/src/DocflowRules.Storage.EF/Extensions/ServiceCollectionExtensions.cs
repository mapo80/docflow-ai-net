using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowRules.Storage.EF;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocflowRulesSqlite(
        this IServiceCollection services,
        string connectionString,
        bool seedBuiltins = true)
    {
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));
        services.AddScoped<IRuleFunctionRepository, RuleFunctionRepository>();
        services.AddScoped<IRuleTestCaseRepository, RuleTestCaseRepository>();
        services.AddScoped<ITestSuiteRepository, TestSuiteRepository>();
        services.AddScoped<ITestTagRepository, TestTagRepository>();

        if (seedBuiltins)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            SeedData.EnsureSeedAsync(db).GetAwaiter().GetResult();
        }

        return services;
    }
}
