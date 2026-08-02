using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultationService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledSlotToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "consultation_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledSlotId",
                table: "consultation_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_consultation_sessions_ScheduledSlotId",
                table: "consultation_sessions",
                column: "ScheduledSlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_consultation_sessions_ScheduledSlotId",
                table: "consultation_sessions");

            migrationBuilder.DropColumn(
                name: "ScheduledSlotId",
                table: "consultation_sessions");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "consultation_sessions");
        }
    }
}
