using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<OrganizationProfile> OrganizationProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Уникальные индексы
            modelBuilder.Entity<User>().HasIndex(u => u.PhoneNumber).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.PublicId).IsUnique();
            modelBuilder.Entity<Profile>().HasIndex(p => new { p.UserId, p.ProfileType }).IsUnique();

            // Связь Profile -> User
            modelBuilder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithMany(u => u.Profiles)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связи Profile -> конкретные профили (один-к-одному)
            modelBuilder.Entity<PatientProfile>()
                .HasOne(pp => pp.Profile)
                .WithOne(p => p.PatientProfile)
                .HasForeignKey<PatientProfile>(pp => pp.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorProfile>()
                .HasOne(dp => dp.Profile)
                .WithOne(p => p.DoctorProfile)
                .HasForeignKey<DoctorProfile>(dp => dp.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationProfile>()
                .HasOne(op => op.Profile)
                .WithOne(p => p.OrganizationProfile)
                .HasForeignKey<OrganizationProfile>(op => op.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationProfile>().OwnsOne(op => op.LegalAddress);

            modelBuilder.Entity<User>()
                .Property(u => u.Sex)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorProfile>()
                .Property(d => d.Category)
                .HasConversion<string>();

            modelBuilder.Entity<OrganizationProfile>()
                .Property(o => o.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Profile>()
                .Property(p => p.ProfileType)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}