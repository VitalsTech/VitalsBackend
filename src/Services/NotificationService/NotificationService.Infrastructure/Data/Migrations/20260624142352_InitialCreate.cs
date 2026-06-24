using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_push_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_push_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                columns: table => new
                {
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_events", x => x.SourceEventId);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VoiceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => new { x.UserId, x.Category });
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_push_tokens_UserId_Token",
                table: "device_push_tokens",
                columns: new[] { "UserId", "Token" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_CreatedAt",
                table: "notification_delivery_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_NextRetryAt",
                table: "notification_delivery_logs",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_SourceEventId",
                table: "notification_delivery_logs",
                column: "SourceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_UserId",
                table: "notification_delivery_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_TemplateKey_Channel_Language_IsActive",
                table: "notification_templates",
                columns: new[] { "TemplateKey", "Channel", "Language", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_processed_events_ProcessedAt",
                table: "processed_events",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_push_tokens");

            migrationBuilder.DropTable(
                name: "notification_delivery_logs");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "processed_events");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");
        }
    }
}
