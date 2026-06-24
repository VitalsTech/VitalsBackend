using AnalyticsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnalyticsService.Infrastructure.Data;

public sealed class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<AnalyticsMetric> Metrics => Set<AnalyticsMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalyticsMetric>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MetricName).HasMaxLength(128);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.HasIndex(x => x.RecordedAt);
            entity.HasIndex(x => x.EventType);
        });
    }
}

public sealed class AnalyticsDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql("Host=localhost;Port=5441;Database=AnalyticsDb;Username=postgres;Password=postgres123")
            .Options);
}
