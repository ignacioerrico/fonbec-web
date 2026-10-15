using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRejectionBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_ApprovedOrRejected",
                table: "Documents");

            migrationBuilder.AddColumn<DateTime>(
                name: "FacilitatorRejectionNotifiedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            // Documents already rejected were decided before this notification existed.
            // Status 5 is Rejected. Mark them sent so startup recovery does not email them.
            migrationBuilder.Sql("""
                UPDATE Documents
                SET FacilitatorRejectionNotifiedOn = COALESCE(RejectedOn, SYSUTCDATETIME())
                WHERE Status = 5
                  AND FacilitatorRejectionNotifiedOn IS NULL
                """);

            migrationBuilder.AddColumn<int>(
                name: "RejectionOverriddenById",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectionOverriddenOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DocumentRejectionBehavior",
                table: "Chapters",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RejectionOverriddenById",
                table: "Documents",
                column: "RejectionOverriddenById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status_FacilitatorRejectionNotifiedOn",
                table: "Documents",
                columns: new[] { "Status", "FacilitatorRejectionNotifiedOn" });

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AspNetUsers_RejectionOverriddenById",
                table: "Documents",
                column: "RejectionOverriddenById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_RejectionOverriddenById",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_RejectionOverriddenById",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Status_FacilitatorRejectionNotifiedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FacilitatorRejectionNotifiedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RejectionOverriddenById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RejectionOverriddenOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentRejectionBehavior",
                table: "Chapters");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_ApprovedOrRejected",
                table: "Documents",
                sql: "[ApprovedOn] IS NULL OR [RejectedOn] IS NULL");
        }
    }
}
