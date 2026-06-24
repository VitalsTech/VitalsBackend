using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinic_routing_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RuleValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinic_routing_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "patient_routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routing_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriageSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeType = table.Column<int>(type: "integer", nullable: false),
                    Specialist = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ConsultationFormat = table.Column<int>(type: "integer", nullable: true),
                    AssignedDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedDoctorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    UrgencyLevel = table.Column<int>(type: "integer", nullable: false),
                    RecommendedLabsJson = table.Column<string>(type: "text", nullable: false),
                    PatientMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routing_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputEventJson = table.Column<string>(type: "text", nullable: false),
                    MedicalContextJson = table.Column<string>(type: "text", nullable: false),
                    RejectedAlternativesJson = table.Column<string>(type: "text", nullable: false),
                    PublishedEventsJson = table.Column<string>(type: "text", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_routing_audit_entries_routing_decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "routing_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clinic_routing_rules_ClinicId_RuleKey",
                table: "clinic_routing_rules",
                columns: new[] { "ClinicId", "RuleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_routes_PatientId_Status",
                table: "patient_routes",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_audit_entries_CreatedAt",
                table: "routing_audit_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_routing_audit_entries_DecisionId",
                table: "routing_audit_entries",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_CreatedAt",
                table: "routing_decisions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_PatientId",
                table: "routing_decisions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_TriageSessionId",
                table: "routing_decisions",
                column: "TriageSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinic_routing_rules");

            migrationBuilder.DropTable(
                name: "patient_routes");

            migrationBuilder.DropTable(
                name: "routing_audit_entries");

            migrationBuilder.DropTable(
                name: "routing_decisions");
        }
    }
}
