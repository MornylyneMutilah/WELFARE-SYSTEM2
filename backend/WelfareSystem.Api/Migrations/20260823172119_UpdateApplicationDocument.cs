using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WelfareSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "ApplicationDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "ApplicationDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "ApplicationDocuments");
        }
    }
}
