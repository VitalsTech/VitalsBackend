using PrescriptionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PrescriptionService.Infrastructure.Data;

public sealed class PrescriptionDbContext : DbContext
{
    public PrescriptionDbContext(DbContextOptions<PrescriptionDbContext> options) : base(options)
    {
    }

    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionMedicationItem> PrescriptionMedications => Set<PrescriptionMedicationItem>();
    public DbSet<PrescriptionStatusHistory> PrescriptionStatusHistory => Set<PrescriptionStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.ToTable("prescriptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DiagnosisForPrescription).HasMaxLength(512);
            entity.Property(x => x.PreferentialCategory).HasMaxLength(128);
            entity.Property(x => x.PharmacistComment).HasMaxLength(2000);
            entity.Property(x => x.ElectronicSignature).HasMaxLength(4096);
            entity.Property(x => x.PharmacyOrderId).HasMaxLength(128);
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ValidUntil);
            entity.HasMany(x => x.Medications).WithOne(x => x.Prescription!).HasForeignKey(x => x.PrescriptionId);
        });

        modelBuilder.Entity<PrescriptionMedicationItem>(entity =>
        {
            entity.ToTable("prescription_medications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TradeName).HasMaxLength(256);
            entity.Property(x => x.Inn).HasMaxLength(256);
            entity.Property(x => x.AtcCode).HasMaxLength(32);
        });

        modelBuilder.Entity<PrescriptionStatusHistory>(entity =>
        {
            entity.ToTable("prescription_status_history");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Initiator).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(512);
            entity.HasIndex(x => x.PrescriptionId);
        });
    }
}
