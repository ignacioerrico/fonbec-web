using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentHistoryVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentHistoryVisits",
                columns: table => new
                {
                    DocumentHistoryVisitId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SponsorId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    LastVisitedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHistoryVisits", x => x.DocumentHistoryVisitId);
                    table.CheckConstraint("CK_DocumentHistoryVisit_RecipientRequired", "([SponsorId] IS NOT NULL AND [CompanyId] IS NULL) OR ([SponsorId] IS NULL AND [CompanyId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_DocumentHistoryVisits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DocumentHistoryVisits_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DocumentHistoryVisits_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistoryVisits_CompanyId_StudentId",
                table: "DocumentHistoryVisits",
                columns: new[] { "CompanyId", "StudentId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistoryVisits_SponsorId_StudentId",
                table: "DocumentHistoryVisits",
                columns: new[] { "SponsorId", "StudentId" },
                unique: true,
                filter: "[SponsorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistoryVisits_StudentId",
                table: "DocumentHistoryVisits",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentHistoryVisits");
        }
    }
}
