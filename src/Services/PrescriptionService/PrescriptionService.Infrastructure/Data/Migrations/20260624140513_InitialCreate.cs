using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrescriptionService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prescription_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Initiator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_status_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPreferential = table.Column<bool>(type: "boolean", nullable: false),
                    PreferentialCategory = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DiagnosisForPrescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PharmacistComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ElectronicSignature = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PharmacyOrderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SelectedPharmacyId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowedRefills = table.Column<int>(type: "integer", nullable: false),
                    UsedRefills = table.Column<int>(type: "integer", nullable: false),
                    AutoRenewalEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PatientInstructionJson = table.Column<string>(type: "text", nullable: false),
                    ValidationResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prescription_medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Inn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DosageForm = table.Column<string>(type: "text", nullable: false),
                    Dosage = table.Column<string>(type: "text", nullable: false),
                    PackageQuantity = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    CourseDays = table.Column<int>(type: "integer", nullable: false),
                    SpecialInstructions = table.Column<string>(type: "text", nullable: true),
                    AtcCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequiresPrescription = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDailyDose = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prescription_medications_prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prescription_medications_PrescriptionId",
                table: "prescription_medications",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_status_history_PrescriptionId",
                table: "prescription_status_history",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_DoctorId",
                table: "prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_PatientId",
                table: "prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_Status",
                table: "prescriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_ValidUntil",
                table: "prescriptions",
                column: "ValidUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prescription_medications");

            migrationBuilder.DropTable(
                name: "prescription_status_history");

            migrationBuilder.DropTable(
                name: "prescriptions");
        }
    }
}
