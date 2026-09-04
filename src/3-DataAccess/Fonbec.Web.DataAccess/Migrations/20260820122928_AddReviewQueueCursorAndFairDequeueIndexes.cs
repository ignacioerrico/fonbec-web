using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewQueueCursorAndFairDequeueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "ReviewQueueCursors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LastServedChapterId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewQueueCursors", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ReviewQueueCursors",
                columns: new[] { "Id", "LastServedChapterId" },
                values: new object[] { 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ChapterId_Status",
                table: "Documents",
                columns: new[] { "ChapterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentQueueItems_Priority_EnqueuedAt",
                table: "DocumentQueueItems",
                columns: new[] { "Priority", "EnqueuedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewQueueCursors");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ChapterId_Status",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentQueueItems_Priority_EnqueuedAt",
                table: "DocumentQueueItems");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents",
                column: "ChapterId");
        }
    }
}
