using NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Data;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationTemplate> Templates => Set<NotificationTemplate>();
    public DbSet<NotificationDeliveryLog> DeliveryLogs => Set<NotificationDeliveryLog>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<DevicePushToken> PushTokens => Set<DevicePushToken>();
    public DbSet<UserNotificationPreference> Preferences => Set<UserNotificationPreference>();
    public DbSet<DeadLetterNotification> DeadLetters => Set<DeadLetterNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("notification_templates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TemplateKey).HasMaxLength(128);
            entity.Property(x => x.Language).HasMaxLength(8);
            entity.Property(x => x.Channel).HasMaxLength(16);
            entity.HasIndex(x => new { x.TemplateKey, x.Channel, x.Language, x.IsActive });
        });

        modelBuilder.Entity<NotificationDeliveryLog>(entity =>
        {
            entity.ToTable("notification_delivery_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.Property(x => x.Channel).HasMaxLength(16);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.SourceEventId);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.NextRetryAt);
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("processed_events");
            entity.HasKey(x => x.SourceEventId);
            entity.HasIndex(x => x.ProcessedAt);
        });

        modelBuilder.Entity<DevicePushToken>(entity =>
        {
            entity.ToTable("device_push_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Platform).HasMaxLength(16);
            entity.Property(x => x.Token).HasMaxLength(512);
            entity.HasIndex(x => new { x.UserId, x.Token });
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("user_notification_preferences");
            entity.HasKey(x => new { x.UserId, x.Category });
            entity.Property(x => x.Category).HasMaxLength(64);
        });

        modelBuilder.Entity<DeadLetterNotification>(entity =>
        {
            entity.ToTable("dead_letter_notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.Property(x => x.Channel).HasMaxLength(16);
            entity.Property(x => x.Priority).HasMaxLength(16);
            entity.HasIndex(x => x.MovedAt);
            entity.HasIndex(x => x.UserId);
        });
    }
}
