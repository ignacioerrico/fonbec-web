using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Documents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Period",
                table: "Documents",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentDescriptionOptions",
                columns: table => new
                {
                    DocumentDescriptionOptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDescriptionOptions", x => x.DocumentDescriptionOptionId);
                    table.ForeignKey(
                        name: "FK_DocumentDescriptionOptions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "DocumentDescriptionOptions",
                columns: new[] { "DocumentDescriptionOptionId", "ChapterId", "DocumentType", "IsActive", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 1, null, (byte)2, true, 1, "Boletín 1º trimestre" },
                    { 2, null, (byte)2, true, 2, "Boletín 2º trimestre" },
                    { 3, null, (byte)2, true, 3, "Boletín 3º trimestre" },
                    { 4, null, (byte)2, true, 4, "Boletín 4º trimestre" },
                    { 5, null, (byte)2, true, 5, "Libreta universitaria" },
                    { 6, null, (byte)3, true, 1, "Certificado de alumno regular" },
                    { 7, null, (byte)3, true, 2, "Constancia" },
                    { 8, null, (byte)3, true, 3, "Foto" },
                    { 9, null, (byte)3, true, 4, "Otro documento" }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_DescriptionRequired",
                table: "Documents",
                sql: "[DocumentType] = 1 OR [Description] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReportCard_PeriodRequired",
                table: "Documents",
                sql: "[DocumentType] <> 2 OR [Period] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDescriptionOptions_ChapterId_DocumentType_Text",
                table: "DocumentDescriptionOptions",
                columns: new[] { "ChapterId", "DocumentType", "Text" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentDescriptionOptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_DescriptionRequired",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReportCard_PeriodRequired",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "Documents");
        }
    }
}
