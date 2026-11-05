using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WelfareSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOperationalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmploymentVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "ApplicationDocuments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequirementName",
                table: "ApplicationDocuments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AccountActionTokens",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountActionTokens", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_AccountActionTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecisionAmendments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReviewId = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<int>(type: "integer", nullable: false),
                    ResolvedBy = table.Column<int>(type: "integer", nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProposedNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReconsiderationApplicationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionAmendments_ApplicationReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "ApplicationReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecisionAmendments_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecisionAmendments_Users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecisionAmendments_WelfareApplications_ReconsiderationAppli~",
                        column: x => x.ReconsiderationApplicationId,
                        principalTable: "WelfareApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetectionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FrequencyDays = table.Column<int>(type: "integer", nullable: false),
                    FrequencyLimit = table.Column<int>(type: "integer", nullable: false),
                    DuplicateDays = table.Column<int>(type: "integer", nullable: false),
                    AmountPercentage = table.Column<double>(type: "double precision", nullable: false),
                    EnableExperimentalModel = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WelfareProgramId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestedBy = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    ProposalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramBudgets_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramBudgets_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramBudgets_WelfarePrograms_WelfareProgramId",
                        column: x => x.WelfareProgramId,
                        principalTable: "WelfarePrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_ContentHash",
                table: "ApplicationDocuments",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_AccountActionTokens_UserId_Purpose_CreatedAt",
                table: "AccountActionTokens",
                columns: new[] { "UserId", "Purpose", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAmendments_ReconsiderationApplicationId",
                table: "DecisionAmendments",
                column: "ReconsiderationApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAmendments_RequestedBy",
                table: "DecisionAmendments",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAmendments_ResolvedBy",
                table: "DecisionAmendments",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAmendments_ReviewId",
                table: "DecisionAmendments",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramBudgets_ApprovedBy",
                table: "ProgramBudgets",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramBudgets_RequestedBy",
                table: "ProgramBudgets",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramBudgets_WelfareProgramId_Year",
                table: "ProgramBudgets",
                columns: new[] { "WelfareProgramId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountActionTokens");

            migrationBuilder.DropTable(
                name: "DecisionAmendments");

            migrationBuilder.DropTable(
                name: "DetectionSettings");

            migrationBuilder.DropTable(
                name: "ProgramBudgets");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationDocuments_ContentHash",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "RequirementName",
                table: "ApplicationDocuments");
        }
    }
}
