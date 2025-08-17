using Microsoft.EntityFrameworkCore;

namespace DocflowAi.Net.Api.Features.Templates;

public class TemplatesDbContext : DbContext
{
    public TemplatesDbContext(DbContextOptions<TemplatesDbContext> options) : base(options) { }
    public DbSet<Template> Templates => Set<Template>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Template>();
        e.HasIndex(t => t.Name).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
