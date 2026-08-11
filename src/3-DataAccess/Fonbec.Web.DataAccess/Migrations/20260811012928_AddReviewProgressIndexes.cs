using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewProgressIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_PlanId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentType_Status",
                table: "Documents",
                columns: new[] { "DocumentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PlanId_ChapterId_Status",
                table: "Documents",
                columns: new[] { "PlanId", "ChapterId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentType_Status",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_PlanId_ChapterId_Status",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PlanId",
                table: "Documents",
                column: "PlanId");
        }
    }
}
