using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAsSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentShares_DocumentId_SponsorId",
                table: "DocumentShares");

            migrationBuilder.DropColumn(
                name: "CompanyNotifiedOn",
                table: "Documents");

            migrationBuilder.AlterColumn<int>(
                name: "SponsorId",
                table: "DocumentShares",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DocumentShares",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicAccessToken",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Give existing companies a unique token so the unique index below is satisfiable.
            migrationBuilder.Sql("UPDATE Companies SET PublicAccessToken = NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_CompanyId",
                table: "DocumentShares",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_DocumentId_CompanyId",
                table: "DocumentShares",
                columns: new[] { "DocumentId", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_DocumentId_SponsorId",
                table: "DocumentShares",
                columns: new[] { "DocumentId", "SponsorId" },
                unique: true,
                filter: "[SponsorId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentShare_RecipientRequired",
                table: "DocumentShares",
                sql: "([SponsorId] IS NOT NULL AND [CompanyId] IS NULL) OR ([SponsorId] IS NULL AND [CompanyId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PublicAccessToken",
                table: "Companies",
                column: "PublicAccessToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentShares_Companies_CompanyId",
                table: "DocumentShares",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentShares_Companies_CompanyId",
                table: "DocumentShares");

            migrationBuilder.DropIndex(
                name: "IX_DocumentShares_CompanyId",
                table: "DocumentShares");

            migrationBuilder.DropIndex(
                name: "IX_DocumentShares_DocumentId_CompanyId",
                table: "DocumentShares");

            migrationBuilder.DropIndex(
                name: "IX_DocumentShares_DocumentId_SponsorId",
                table: "DocumentShares");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentShare_RecipientRequired",
                table: "DocumentShares");

            migrationBuilder.DropIndex(
                name: "IX_Companies_PublicAccessToken",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DocumentShares");

            migrationBuilder.DropColumn(
                name: "PublicAccessToken",
                table: "Companies");

            migrationBuilder.AlterColumn<int>(
                name: "SponsorId",
                table: "DocumentShares",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompanyNotifiedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_DocumentId_SponsorId",
                table: "DocumentShares",
                columns: new[] { "DocumentId", "SponsorId" },
                unique: true);
        }
    }
}
