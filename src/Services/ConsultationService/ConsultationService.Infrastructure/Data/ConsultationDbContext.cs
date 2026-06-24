using ConsultationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConsultationService.Infrastructure.Data;

public sealed class ConsultationDbContext : DbContext
{
    public ConsultationDbContext(DbContextOptions<ConsultationDbContext> options) : base(options)
    {
    }

    public DbSet<ConsultationSession> Sessions => Set<ConsultationSession>();
    public DbSet<ConsultationMessage> Messages => Set<ConsultationMessage>();
    public DbSet<SessionStatusTransition> StatusTransitions => Set<SessionStatusTransition>();
    public DbSet<SessionParticipant> Participants => Set<SessionParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsultationSession>(entity =>
        {
            entity.ToTable("consultation_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DoctorName).HasMaxLength(256);
            entity.Property(x => x.CancelReason).HasMaxLength(512);
            entity.Property(x => x.PatientFeedback).HasMaxLength(2000);
            entity.Property(x => x.DoctorFeedback).HasMaxLength(2000);
            entity.Property(x => x.VideoRoomId).HasMaxLength(128);
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ConsultationMessage>(entity =>
        {
            entity.ToTable("consultation_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).HasMaxLength(8000);
            entity.Property(x => x.AttachmentUrl).HasMaxLength(1024);
            entity.HasIndex(x => new { x.SessionId, x.SequenceNumber }).IsUnique();
            entity.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionStatusTransition>(entity =>
        {
            entity.ToTable("session_status_transitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Initiator).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(512);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<SessionParticipant>(entity =>
        {
            entity.ToTable("session_participants");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SessionId, x.UserId });
        });
    }
}
