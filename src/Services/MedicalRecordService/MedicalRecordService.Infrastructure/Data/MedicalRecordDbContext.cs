using MedicalRecordService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Infrastructure.Data;

public class MedicalRecordDbContext : DbContext
{
    public MedicalRecordDbContext(DbContextOptions<MedicalRecordDbContext> options) : base(options) { }

    public DbSet<MedicalEvent> MedicalEvents => Set<MedicalEvent>();
    public DbSet<PatientSnapshot> PatientSnapshots => Set<PatientSnapshot>();
    public DbSet<PatientDiagnosisProjection> Diagnoses => Set<PatientDiagnosisProjection>();
    public DbSet<PatientPrescriptionProjection> Prescriptions => Set<PatientPrescriptionProjection>();
    public DbSet<PatientAllergyProjection> Allergies => Set<PatientAllergyProjection>();
    public DbSet<PatientImmunizationProjection> Immunizations => Set<PatientImmunizationProjection>();
    public DbSet<PatientVitalSignProjection> VitalSigns => Set<PatientVitalSignProjection>();
    public DbSet<PatientLabResultProjection> LabResults => Set<PatientLabResultProjection>();
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalEvent>(e =>
        {
            e.ToTable("medical_events");
            e.HasKey(x => x.EventId);
            e.HasIndex(x => new { x.PatientId, x.Version }).IsUnique();
            e.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("jsonb");
            e.Property(x => x.SourceService).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<PatientSnapshot>(e =>
        {
            e.ToTable("patient_snapshots");
            e.HasIndex(x => new { x.PatientId, x.UpToVersion });
            e.Property(x => x.StateJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<PatientDiagnosisProjection>(e =>
        {
            e.ToTable("projection_diagnoses");
            e.HasIndex(x => new { x.PatientId, x.IsActive });
        });

        modelBuilder.Entity<PatientPrescriptionProjection>(e =>
        {
            e.ToTable("projection_prescriptions");
            e.HasIndex(x => new { x.PatientId, x.IsActive });
        });

        modelBuilder.Entity<PatientAllergyProjection>(e => e.ToTable("projection_allergies"));
        modelBuilder.Entity<PatientImmunizationProjection>(e => e.ToTable("projection_immunizations"));
        modelBuilder.Entity<PatientVitalSignProjection>(e =>
        {
            e.ToTable("projection_vitals");
            e.HasIndex(x => new { x.PatientId, x.RecordedAt });
        });

        modelBuilder.Entity<PatientLabResultProjection>(e =>
        {
            e.ToTable("projection_lab_results");
            e.HasIndex(x => new { x.PatientId, x.ReceivedAt });
        });

        modelBuilder.Entity<AccessGrant>(e =>
        {
            e.ToTable("access_grants");
            e.HasIndex(x => new { x.PatientId, x.GranteeId, x.RevokedAt });
        });

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.ToTable("audit_logs");
            e.HasIndex(x => new { x.PatientId, x.OccurredAt });
        });
    }
}
