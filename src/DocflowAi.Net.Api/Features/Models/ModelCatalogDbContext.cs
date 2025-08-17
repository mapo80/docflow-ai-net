using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Models;

public class ModelCatalogDbContext : DbContext
{
    public DbSet<GgufModel> Models => Set<GgufModel>();

    public ModelCatalogDbContext(DbContextOptions<ModelCatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GgufModel>()
            .HasIndex(m => m.Name)
            .IsUnique(false);

        modelBuilder.Entity<GgufModel>()
            .HasIndex(m => m.IsActive);

        modelBuilder.Entity<GgufModel>()
            .Property(m => m.Status)
            .HasConversion<int>();

        base.OnModelCreating(modelBuilder);
    }
}
