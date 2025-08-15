using DocflowAi.Net.Api.JobQueue.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocflowAi.Net.Api.JobQueue.Data;

public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options) { }

    public DbSet<JobDocument> Jobs => Set<JobDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<JobDocument>();
        var converter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        var nullableConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null);

        job.HasKey(j => j.Id);
        job.HasIndex(j => j.CreatedAt);
        job.HasIndex(j => new { j.Status, j.AvailableAt });
        job.HasIndex(j => j.IdempotencyKey);
        job.HasIndex(j => j.Hash);

        job.Property(j => j.CreatedAt).HasConversion(converter);
        job.Property(j => j.UpdatedAt).HasConversion(converter);
        job.Property(j => j.AvailableAt).HasConversion(nullableConverter);
        job.Property(j => j.LeaseUntil).HasConversion(nullableConverter);

        job.OwnsOne(j => j.Metrics, m =>
        {
            m.Property(x => x.StartedAt).HasConversion(nullableConverter);
            m.Property(x => x.EndedAt).HasConversion(nullableConverter);
        });
        job.OwnsOne(j => j.Paths);
    }
}
