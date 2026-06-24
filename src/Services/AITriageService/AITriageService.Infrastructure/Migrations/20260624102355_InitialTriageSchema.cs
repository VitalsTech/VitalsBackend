using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITriageService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTriageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "triage_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LatestUrgencyLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_triage_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "triage_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrgencyLevel = table.Column<int>(type: "integer", nullable: false),
                    ExtractedEntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    NerEntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    LlmResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    AssistantReply = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_triage_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_triage_assessments_triage_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "triage_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "triage_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_triage_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_triage_messages_triage_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "triage_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_triage_assessments_SessionId",
                table: "triage_assessments",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_triage_messages_SessionId",
                table: "triage_messages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_triage_sessions_PatientId",
                table: "triage_sessions",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "triage_assessments");

            migrationBuilder.DropTable(
                name: "triage_messages");

            migrationBuilder.DropTable(
                name: "triage_sessions");
        }
    }
}
