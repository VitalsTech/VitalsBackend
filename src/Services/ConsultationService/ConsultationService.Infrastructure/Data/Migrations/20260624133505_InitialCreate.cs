using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultationService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consultation_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UrgencyLevel = table.Column<int>(type: "integer", nullable: false),
                    ExpectedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RoutingDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriageSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    PatientConsentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoRecordingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    PatientUnreadCount = table.Column<int>(type: "integer", nullable: false),
                    DoctorUnreadCount = table.Column<int>(type: "integer", nullable: false),
                    PatientRating = table.Column<int>(type: "integer", nullable: true),
                    PatientFeedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DoctorRating = table.Column<int>(type: "integer", nullable: true),
                    DoctorFeedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    VideoRoomId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProtocolJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultation_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_status_transitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Initiator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_status_transitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consultation_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderRole = table.Column<int>(type: "integer", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsImportant = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultation_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consultation_messages_consultation_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "consultation_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consultation_messages_SessionId_SequenceNumber",
                table: "consultation_messages",
                columns: new[] { "SessionId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consultation_sessions_CreatedAt",
                table: "consultation_sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_consultation_sessions_DoctorId",
                table: "consultation_sessions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_consultation_sessions_PatientId",
                table: "consultation_sessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_consultation_sessions_Status",
                table: "consultation_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_session_participants_SessionId_UserId",
                table: "session_participants",
                columns: new[] { "SessionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_session_status_transitions_OccurredAt",
                table: "session_status_transitions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_session_status_transitions_SessionId",
                table: "session_status_transitions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consultation_messages");

            migrationBuilder.DropTable(
                name: "session_participants");

            migrationBuilder.DropTable(
                name: "session_status_transitions");

            migrationBuilder.DropTable(
                name: "consultation_sessions");
        }
    }
}
