using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    SecondName = table.Column<string>(type: "text", nullable: true),
                    Surename = table.Column<string>(type: "text", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sex = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Specialization = table.Column<string>(type: "text", nullable: false),
                    DiplomaNumber = table.Column<string>(type: "text", nullable: false),
                    DiplomaSeries = table.Column<string>(type: "text", nullable: true),
                    CertificateNumber = table.Column<string>(type: "text", nullable: false),
                    CertificateExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    AcademicDegree = table.Column<string>(type: "text", nullable: true),
                    Biography = table.Column<string>(type: "text", nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    PatientIdsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    INN = table.Column<string>(type: "text", nullable: false),
                    KPP = table.Column<string>(type: "text", nullable: true),
                    OGRN = table.Column<string>(type: "text", nullable: false),
                    LegalAddress_PostCode = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_Country = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_Region = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_City = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_Area = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_Street = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_House = table.Column<string>(type: "text", nullable: true),
                    LegalAddress_Flat = table.Column<string>(type: "text", nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    AdministratorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceNumber = table.Column<string>(type: "text", nullable: true),
                    SNILS = table.Column<string>(type: "text", nullable: true),
                    BloodType = table.Column<int>(type: "integer", nullable: true),
                    Allergies = table.Column<string>(type: "text", nullable: true),
                    DoctorIdsJson = table.Column<string>(type: "text", nullable: false),
                    OrganizationIdsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Name", "Resource" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Read", new DateTime(2026, 5, 14, 14, 26, 28, 955, DateTimeKind.Utc).AddTicks(9740), "Просмотр своей медицинской истории", "self.history.read", "History" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "View", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(788), "Просмотр своего профиля", "self.profile.view", "Profile" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Edit", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(797), "Редактирование своего профиля", "self.profile.edit", "Profile" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Read", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(802), "Просмотр истории пациента (для врача)", "patient.history.read", "History" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Create", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(807), "Выписка рецептов", "prescription.create", "Prescription" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "View", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(815), "Просмотр рецептов", "prescription.view", "Prescription" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Dispense", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(821), "Выдача лекарств по рецепту", "prescription.dispense", "Prescription" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Create", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(825), "Создание заказа на анализы", "lab.order.create", "Lab" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Upload", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(830), "Загрузка результатов анализов", "lab.results.upload", "Lab" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "View", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(835), "Просмотр результатов анализов", "lab.results.view", "Lab" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Start", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(840), "Начало консультации", "consultation.start", "Consultation" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "Join", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(844), "Подключение к консультации", "consultation.join", "Consultation" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Manage", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(849), "Управление пользователями организации", "org.users.manage", "Organization" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "Manage", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(853), "Управление расписанием организации", "org.schedule.manage", "Organization" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "View", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(858), "Просмотр финансовой отчетности", "org.finance.view", "Organization" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "ViewAll", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(862), "Просмотр всех пользователей", "users.view.all", "Users" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "Block", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(866), "Блокировка пользователей", "users.block", "Users" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "Manage", new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(872), "Управление настройками системы", "system.settings.manage", "System" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsSystem", "Name", "OrganizationId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(6410), "Обычный пациент", true, "Patient", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7239), "Врач", true, "Doctor", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7246), "Администратор клиники", true, "ClinicAdmin", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7252), "Сотрудник лаборатории", true, "LabEmployee", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7257), "Сотрудник аптеки", true, "PharmacyEmployee", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7265), "Администратор платформы", true, "PlatformAdmin", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTime(2026, 5, 14, 14, 26, 28, 956, DateTimeKind.Utc).AddTicks(7270), "Суперадминистратор", true, "SuperAdmin", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_ProfileType",
                table: "Profiles",
                columns: new[] { "UserId", "ProfileType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_ProfileId",
                table: "UserRoles",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PublicId",
                table: "Users",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProfiles");

            migrationBuilder.DropTable(
                name: "OrganizationProfiles");

            migrationBuilder.DropTable(
                name: "PatientProfiles");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
