using AITriageService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AITriageService.Infrastructure.Data;

public class TriageDbContext : DbContext
{
    public TriageDbContext(DbContextOptions<TriageDbContext> options) : base(options) { }

    public DbSet<TriageSession> Sessions => Set<TriageSession>();
    public DbSet<TriageMessage> Messages => Set<TriageMessage>();
    public DbSet<TriageAssessment> Assessments => Set<TriageAssessment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TriageSession>(e =>
        {
            e.ToTable("triage_sessions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PatientId);
        });

        modelBuilder.Entity<TriageMessage>(e =>
        {
            e.ToTable("triage_messages");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Session).WithMany(x => x.Messages).HasForeignKey(x => x.SessionId);
        });

        modelBuilder.Entity<TriageAssessment>(e =>
        {
            e.ToTable("triage_assessments");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExtractedEntitiesJson).HasColumnType("jsonb");
            e.Property(x => x.NerEntitiesJson).HasColumnType("jsonb");
            e.Property(x => x.LlmResultJson).HasColumnType("jsonb");
            e.HasOne(x => x.Session).WithMany(x => x.Assessments).HasForeignKey(x => x.SessionId);
        });
    }
}
