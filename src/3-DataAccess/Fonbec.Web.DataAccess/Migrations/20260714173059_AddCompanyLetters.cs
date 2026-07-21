using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_StudentId_SponsorId_PlanId",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Letter_SponsorRequired",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompanyNotifiedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId_CompanyId_PlanId",
                table: "Documents",
                columns: new[] { "StudentId", "CompanyId", "PlanId" },
                unique: true,
                filter: "[DocumentType] = 1 AND [Status] <> 5 AND [CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId_SponsorId_PlanId",
                table: "Documents",
                columns: new[] { "StudentId", "SponsorId", "PlanId" },
                unique: true,
                filter: "[DocumentType] = 1 AND [Status] <> 5 AND [SponsorId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Letter_RecipientRequired",
                table: "Documents",
                sql: "[DocumentType] <> 1 OR (([SponsorId] IS NOT NULL AND [CompanyId] IS NULL) OR ([SponsorId] IS NULL AND [CompanyId] IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OtherDocument_CompanyNull",
                table: "Documents",
                sql: "[DocumentType] <> 3 OR [CompanyId] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReportCard_CompanyNull",
                table: "Documents",
                sql: "[DocumentType] <> 2 OR [CompanyId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_StudentId_CompanyId_PlanId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_StudentId_SponsorId_PlanId",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Letter_RecipientRequired",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OtherDocument_CompanyNull",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReportCard_CompanyNull",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CompanyNotifiedOn",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId_SponsorId_PlanId",
                table: "Documents",
                columns: new[] { "StudentId", "SponsorId", "PlanId" },
                unique: true,
                filter: "[DocumentType] = 1 AND [Status] <> 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Letter_SponsorRequired",
                table: "Documents",
                sql: "[DocumentType] <> 1 OR [SponsorId] IS NOT NULL");
        }
    }
}
