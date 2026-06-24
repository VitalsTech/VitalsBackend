using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using QualityService.Domain.Entities;

namespace QualityService.Infrastructure.Data;

public sealed class QualityDbContext : DbContext
{
    public QualityDbContext(DbContextOptions<QualityDbContext> options) : base(options) { }

    public DbSet<QualityScore> Scores => Set<QualityScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QualityScore>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.RecordedAt);
        });
    }
}

public sealed class QualityDbContextFactory : IDesignTimeDbContextFactory<QualityDbContext>
{
    public QualityDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<QualityDbContext>()
            .UseNpgsql("Host=localhost;Port=5442;Database=QualityDb;Username=postgres;Password=postgres123")
            .Options);
}
