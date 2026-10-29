using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WelfareSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anomalies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnomalyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<int>(type: "integer", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    DetectionDetails = table.Column<string>(type: "text", nullable: true),
                    AnomalyScore = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anomalies_Users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anomalies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anomalies_WelfareApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "WelfareApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnomalyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ThresholdConfig = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnomalyRules_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyRules_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleAccessRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RequestedRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAccessRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnomalyId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AlertContent = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnomalyAlerts_Anomalies_AnomalyId",
                        column: x => x.AnomalyId,
                        principalTable: "Anomalies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_AnomalyType",
                table: "Anomalies",
                column: "AnomalyType");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_ApplicationId",
                table: "Anomalies",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_DetectedAt",
                table: "Anomalies",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_ResolvedBy",
                table: "Anomalies",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_Severity",
                table: "Anomalies",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_Status",
                table: "Anomalies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Anomalies_UserId",
                table: "Anomalies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyAlerts_AlertType",
                table: "AnomalyAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyAlerts_AnomalyId",
                table: "AnomalyAlerts",
                column: "AnomalyId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyAlerts_Status",
                table: "AnomalyAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyRules_AnomalyType",
                table: "AnomalyRules",
                column: "AnomalyType");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyRules_CreatedBy",
                table: "AnomalyRules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyRules_IsActive",
                table: "AnomalyRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyRules_UpdatedBy",
                table: "AnomalyRules",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderKey",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessRequests_UserId",
                table: "RoleAccessRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnomalyAlerts");

            migrationBuilder.DropTable(
                name: "AnomalyRules");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "RoleAccessRequests");

            migrationBuilder.DropTable(
                name: "Anomalies");
        }
    }
}
