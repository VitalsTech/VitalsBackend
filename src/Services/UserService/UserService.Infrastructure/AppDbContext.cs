using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UserService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<DoctorScheduleSlot> DoctorScheduleSlots { get; set; }
        public DbSet<OrganizationProfile> OrganizationProfiles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка UserRole (многие ко многим между User, Role и Profile)
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId, ur.ProfileId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Profile)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(ur => ur.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка RolePermission (многие ко многим между Role и Permission)
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime?, DateTime?>(
                            v => v.HasValue ? v.Value.ToUniversalTime() : v,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                    }
                }
            }
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
            modelBuilder.Entity<PatientProfile>().OwnsOne(pp => pp.ResidenceAddress);
            modelBuilder.Entity<PatientProfile>().OwnsOne(pp => pp.RegistrationAddress);

            modelBuilder.Entity<User>()
                .Property(u => u.Sex)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorProfile>()
                .Property(d => d.Category)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorScheduleSlot>()
                .HasIndex(s => new { s.DoctorProfileId, s.StartsAt });

            modelBuilder.Entity<DoctorScheduleSlot>()
                .HasIndex(s => s.ConsultationSessionId);

            modelBuilder.Entity<DoctorScheduleSlot>()
                .HasOne(s => s.DoctorProfile)
                .WithMany()
                .HasForeignKey(s => s.DoctorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationProfile>()
                .Property(o => o.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Profile>()
                .Property(p => p.ProfileType)
                .HasConversion<string>();

            // Добавление базовых прав (permissions)
            var basePermissions = new List<Permission>
            {
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "self.history.read", Resource = "History", Action = "Read", Description = "Просмотр своей медицинской истории" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "self.profile.view", Resource = "Profile", Action = "View", Description = "Просмотр своего профиля" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "self.profile.edit", Resource = "Profile", Action = "Edit", Description = "Редактирование своего профиля" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "patient.history.read", Resource = "History", Action = "Read", Description = "Просмотр истории пациента (для врача)" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "prescription.create", Resource = "Prescription", Action = "Create", Description = "Выписка рецептов" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "prescription.view", Resource = "Prescription", Action = "View", Description = "Просмотр рецептов" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Name = "prescription.dispense", Resource = "Prescription", Action = "Dispense", Description = "Выдача лекарств по рецепту" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Name = "lab.order.create", Resource = "Lab", Action = "Create", Description = "Создание заказа на анализы" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Name = "lab.results.upload", Resource = "Lab", Action = "Upload", Description = "Загрузка результатов анализов" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Name = "lab.results.view", Resource = "Lab", Action = "View", Description = "Просмотр результатов анализов" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), Name = "consultation.start", Resource = "Consultation", Action = "Start", Description = "Начало консультации" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), Name = "consultation.join", Resource = "Consultation", Action = "Join", Description = "Подключение к консультации" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000013"), Name = "org.users.manage", Resource = "Organization", Action = "Manage", Description = "Управление пользователями организации" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000014"), Name = "org.schedule.manage", Resource = "Organization", Action = "Manage", Description = "Управление расписанием организации" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000015"), Name = "org.finance.view", Resource = "Organization", Action = "View", Description = "Просмотр финансовой отчетности" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000016"), Name = "users.view.all", Resource = "Users", Action = "ViewAll", Description = "Просмотр всех пользователей" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000017"), Name = "users.block", Resource = "Users", Action = "Block", Description = "Блокировка пользователей" },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000018"), Name = "system.settings.manage", Resource = "System", Action = "Manage", Description = "Управление настройками системы" }
            };

            modelBuilder.Entity<Permission>().HasData(basePermissions);

            // Добавление базовых ролей
            var baseRoles = new List<Role>
            {
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Patient", Description = "Обычный пациент", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Doctor", Description = "Врач", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "ClinicAdmin", Description = "Администратор клиники", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "LabEmployee", Description = "Сотрудник лаборатории", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Name = "PharmacyEmployee", Description = "Сотрудник аптеки", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Name = "PlatformAdmin", Description = "Администратор платформы", IsSystem = true },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), Name = "SuperAdmin", Description = "Суперадминистратор", IsSystem = true }
            };

            modelBuilder.Entity<Role>().HasData(baseRoles);

            base.OnModelCreating(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}