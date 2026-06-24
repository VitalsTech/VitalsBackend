using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultationService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProtocolSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtocolSignature",
                table: "consultation_sessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtocolSignature",
                table: "consultation_sessions");
        }
    }
}
