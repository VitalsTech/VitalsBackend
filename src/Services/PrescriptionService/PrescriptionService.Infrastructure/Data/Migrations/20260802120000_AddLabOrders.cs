using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrescriptionService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLabOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lab_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClinicalIndication = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DoctorComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExternalLabOrderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TestCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SpecimenType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ReferenceRange = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    ResultReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultAttachmentUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ResultComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lab_order_items_lab_orders_LabOrderId",
                        column: x => x.LabOrderId,
                        principalTable: "lab_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Initiator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_order_status_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_ConsultationId",
                table: "lab_orders",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_DoctorId",
                table: "lab_orders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_PatientId",
                table: "lab_orders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_Status",
                table: "lab_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_items_LabOrderId",
                table: "lab_order_items",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_status_history_LabOrderId",
                table: "lab_order_status_history",
                column: "LabOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "lab_order_items");
            migrationBuilder.DropTable(name: "lab_order_status_history");
            migrationBuilder.DropTable(name: "lab_orders");
        }
    }
}
