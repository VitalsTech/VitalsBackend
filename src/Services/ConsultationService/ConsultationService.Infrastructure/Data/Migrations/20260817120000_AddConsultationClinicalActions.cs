using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultationService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationClinicalActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consultation_clinical_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByDoctorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultation_clinical_actions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consultation_clinical_actions_CreatedAt",
                table: "consultation_clinical_actions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_consultation_clinical_actions_SessionId",
                table: "consultation_clinical_actions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consultation_clinical_actions");
        }
    }
}
