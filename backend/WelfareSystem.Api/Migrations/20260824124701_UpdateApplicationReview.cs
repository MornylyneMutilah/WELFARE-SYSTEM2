using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WelfareSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviews_ReviewerId",
                table: "ApplicationReviews",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationReviews_Users_ReviewerId",
                table: "ApplicationReviews",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationReviews_Users_ReviewerId",
                table: "ApplicationReviews");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationReviews_ReviewerId",
                table: "ApplicationReviews");
        }
    }
}
