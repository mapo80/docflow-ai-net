using DocflowAi.Net.Api.Features.Models.Downloaders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DocflowAi.Net.Api.Features.Models;

public static class ModelsExtensions
{
    public static IServiceCollection AddModelCatalog(this IServiceCollection services, IConfiguration cfg)
    {
        var conn = cfg.GetConnectionString("ModelCatalog")
                   ?? cfg.GetConnectionString("Default")
                   ?? "Data Source=data/models.db";
        services.AddDbContext<ModelCatalogDbContext>(o => o.UseSqlite(conn));

        services.AddHttpClient(); // used by downloaders

        services.AddSingleton<ModelRuntimeManager>();
        services.AddSingleton<IModelActivator2>(new DelegateModelActivator2((payload, ct) => Task.CompletedTask));
        services.AddSingleton<IModelActivator>(new DelegateModelActivator((path, ct) => Task.CompletedTask)); // no-op by default

        services.AddScoped<Downloaders.IModelDownloader, HttpModelDownloader>();
        services.AddScoped<Downloaders.IModelDownloader, HuggingFaceDownloader>();

        services.AddSingleton<ModelDownloadWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<ModelDownloadWorker>());

        // Ensure DB file exists on startup
        using var scope = services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ModelCatalogDbContext>();
        db.Database.EnsureCreated();

        return services;
    }
}
