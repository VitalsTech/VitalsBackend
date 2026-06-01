using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EsiaLink> EsiaLinks => Set<EsiaLink>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.ToTable("auth_users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NormalizedPhone).IsUnique();
            entity.HasIndex(x => x.UserPublicId).IsUnique();
            entity.Property(x => x.NormalizedPhone).HasMaxLength(20).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(64).IsRequired();
            entity.HasOne(x => x.AuthUser)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.AuthUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EsiaLink>(entity =>
        {
            entity.ToTable("esia_links");
            entity.HasKey(x => x.AuthUserId);
            entity.HasIndex(x => x.EsiaSubjectId).IsUnique();
            entity.Property(x => x.EsiaSubjectId).HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.AuthUser)
                .WithOne(x => x.EsiaLink)
                .HasForeignKey<EsiaLink>(x => x.AuthUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(16).IsRequired();
            entity.HasOne(x => x.AuthUser)
                .WithMany(x => x.PasswordResetTokens)
                .HasForeignKey(x => x.AuthUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
