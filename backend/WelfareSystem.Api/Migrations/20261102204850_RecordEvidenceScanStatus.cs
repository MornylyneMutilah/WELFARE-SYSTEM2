using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WelfareSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecordEvidenceScanStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "ApplicationDocuments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "ApplicationDocuments");
        }
    }
}
