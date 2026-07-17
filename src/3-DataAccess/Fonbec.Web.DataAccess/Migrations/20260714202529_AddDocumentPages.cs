using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_BlobPaths_BlobPathId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_BlobPaths_ImprovedBlobPathId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_BlobPaths_OriginalBlobPathId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_BlobPathId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ImprovedBlobPathId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_OriginalBlobPathId",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_ImprovementComplete",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_ImprovementNotApplicable",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BlobPathId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ImprovedBlobPathId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OriginalBlobPathId",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "DocumentPages",
                columns: table => new
                {
                    DocumentPageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalBlobPathId = table.Column<long>(type: "bigint", nullable: false),
                    ImprovedBlobPathId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPages", x => x.DocumentPageId);
                    table.ForeignKey(
                        name: "FK_DocumentPages_BlobPaths_ImprovedBlobPathId",
                        column: x => x.ImprovedBlobPathId,
                        principalTable: "BlobPaths",
                        principalColumn: "BlobPathId");
                    table.ForeignKey(
                        name: "FK_DocumentPages_BlobPaths_OriginalBlobPathId",
                        column: x => x.OriginalBlobPathId,
                        principalTable: "BlobPaths",
                        principalColumn: "BlobPathId");
                    table.ForeignKey(
                        name: "FK_DocumentPages_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPages_DocumentId_PageNumber",
                table: "DocumentPages",
                columns: new[] { "DocumentId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPages_ImprovedBlobPathId",
                table: "DocumentPages",
                column: "ImprovedBlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPages_OriginalBlobPathId",
                table: "DocumentPages",
                column: "OriginalBlobPathId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentPages");

            migrationBuilder.AddColumn<long>(
                name: "BlobPathId",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ImprovedBlobPathId",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OriginalBlobPathId",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_BlobPathId",
                table: "Documents",
                column: "BlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ImprovedBlobPathId",
                table: "Documents",
                column: "ImprovedBlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OriginalBlobPathId",
                table: "Documents",
                column: "OriginalBlobPathId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_ImprovementComplete",
                table: "Documents",
                sql: "[DigitalImprovementStatus] <> 3 OR [ImprovedBlobPathId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_ImprovementNotApplicable",
                table: "Documents",
                sql: "[DigitalImprovementStatus] <> 0 OR [ImprovedBlobPathId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_BlobPaths_BlobPathId",
                table: "Documents",
                column: "BlobPathId",
                principalTable: "BlobPaths",
                principalColumn: "BlobPathId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_BlobPaths_ImprovedBlobPathId",
                table: "Documents",
                column: "ImprovedBlobPathId",
                principalTable: "BlobPaths",
                principalColumn: "BlobPathId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_BlobPaths_OriginalBlobPathId",
                table: "Documents",
                column: "OriginalBlobPathId",
                principalTable: "BlobPaths",
                principalColumn: "BlobPathId");
        }
    }
}
