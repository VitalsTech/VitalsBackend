using RoutingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace RoutingService.Infrastructure.Data;

public sealed class RoutingDbContext : DbContext
{
    public RoutingDbContext(DbContextOptions<RoutingDbContext> options) : base(options)
    {
    }

    public DbSet<RoutingDecision> RoutingDecisions => Set<RoutingDecision>();
    public DbSet<RoutingAuditEntry> RoutingAuditEntries => Set<RoutingAuditEntry>();
    public DbSet<PatientRoute> PatientRoutes => Set<PatientRoute>();
    public DbSet<ClinicRoutingRule> ClinicRoutingRules => Set<ClinicRoutingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoutingDecision>(entity =>
        {
            entity.ToTable("routing_decisions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Specialist).HasMaxLength(64);
            entity.Property(x => x.AssignedDoctorName).HasMaxLength(256);
            entity.Property(x => x.PatientMessage).HasMaxLength(2000);
            entity.Property(x => x.Rationale).HasMaxLength(4000);
            entity.Property(x => x.AlgorithmVersion).HasMaxLength(64);
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.TriageSessionId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<RoutingAuditEntry>(entity =>
        {
            entity.ToTable("routing_audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AlgorithmVersion).HasMaxLength(64);
            entity.HasOne(x => x.Decision)
                .WithMany()
                .HasForeignKey(x => x.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.DecisionId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<PatientRoute>(entity =>
        {
            entity.ToTable("patient_routes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasIndex(x => new { x.PatientId, x.Status });
        });

        modelBuilder.Entity<ClinicRoutingRule>(entity =>
        {
            entity.ToTable("clinic_routing_rules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClinicId).HasMaxLength(64);
            entity.Property(x => x.RuleKey).HasMaxLength(128);
            entity.Property(x => x.RuleValue).HasMaxLength(512);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.HasIndex(x => new { x.ClinicId, x.RuleKey }).IsUnique();
        });
    }
}
