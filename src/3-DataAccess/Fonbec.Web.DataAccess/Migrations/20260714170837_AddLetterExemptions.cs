using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterExemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LetterExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PlannedDeliveryId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedByFonbecUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedByFonbecUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LetterExemptions_AspNetUsers_CreatedByFonbecUserId",
                        column: x => x.CreatedByFonbecUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LetterExemptions_AspNetUsers_RevokedByFonbecUserId",
                        column: x => x.RevokedByFonbecUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LetterExemptions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LetterExemptions_PlannedDeliveries_PlannedDeliveryId",
                        column: x => x.PlannedDeliveryId,
                        principalTable: "PlannedDeliveries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LetterExemptions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LetterExemptions_ChapterId",
                table: "LetterExemptions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterExemptions_CreatedByFonbecUserId",
                table: "LetterExemptions",
                column: "CreatedByFonbecUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterExemptions_PlannedDeliveryId_ChapterId_IsRevoked",
                table: "LetterExemptions",
                columns: new[] { "PlannedDeliveryId", "ChapterId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_LetterExemptions_RevokedByFonbecUserId",
                table: "LetterExemptions",
                column: "RevokedByFonbecUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterExemptions_StudentId_PlannedDeliveryId",
                table: "LetterExemptions",
                columns: new[] { "StudentId", "PlannedDeliveryId" },
                unique: true,
                filter: "[IsRevoked] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetterExemptions");
        }
    }
}
