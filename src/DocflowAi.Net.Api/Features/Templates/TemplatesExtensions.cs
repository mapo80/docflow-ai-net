using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocflowAi.Net.Api.Features.Templates;

public static class TemplatesExtensions
{
    public static IServiceCollection AddTemplatesFeature(this IServiceCollection services, IConfiguration cfg)
    {
        var connTpl = cfg.GetConnectionString("Templates") ?? "Data Source=data/templates.db";
        var connRec = cfg.GetConnectionString("Recognitions") ?? "Data Source=data/recognitions.db";

        services.AddDbContext<TemplatesDbContext>(o => o.UseSqlite(connTpl));
        services.AddDbContext<RecognitionsDbContext>(o => o.UseSqlite(connRec));

        // Ensure created
        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<TemplatesDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<RecognitionsDbContext>().Database.EnsureCreated();
        return services;
    }
}
