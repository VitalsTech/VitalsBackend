using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorScheduleSlotBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "DoctorScheduleSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationSessionId",
                table: "DoctorScheduleSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BookedAt",
                table: "DoctorScheduleSlots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorScheduleSlots_ConsultationSessionId",
                table: "DoctorScheduleSlots",
                column: "ConsultationSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorScheduleSlots_ConsultationSessionId",
                table: "DoctorScheduleSlots");

            migrationBuilder.DropColumn(
                name: "BookedAt",
                table: "DoctorScheduleSlots");

            migrationBuilder.DropColumn(
                name: "ConsultationSessionId",
                table: "DoctorScheduleSlots");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "DoctorScheduleSlots");
        }
    }
}
