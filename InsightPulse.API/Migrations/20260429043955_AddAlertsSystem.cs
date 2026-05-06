using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Metric = table.Column<string>(type: "text", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    SlackWebhookUrl = table.Column<string>(type: "text", nullable: true),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardAlerts_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    WasTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertHistories_DashboardAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "DashboardAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_alert_history",
                table: "AlertHistories",
                columns: new[] { "TenantId", "AlertId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistories_AlertId",
                table: "AlertHistories",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "idx_alerts_tenant_dashboard",
                table: "DashboardAlerts",
                columns: new[] { "TenantId", "DashboardId" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardAlerts_DashboardId",
                table: "DashboardAlerts",
                column: "DashboardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertHistories");

            migrationBuilder.DropTable(
                name: "DashboardAlerts");
        }
    }
}
