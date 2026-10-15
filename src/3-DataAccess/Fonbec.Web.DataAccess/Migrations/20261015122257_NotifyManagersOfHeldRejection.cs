using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NotifyManagersOfHeldRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ManagersNotifiedOfHeldRejectionOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            // Status 6 is PendingManagerDecision. Documents already waiting were held
            // before this notification existed, so mark them sent.
            migrationBuilder.Sql("""
                UPDATE Documents
                SET ManagersNotifiedOfHeldRejectionOn = COALESCE(RejectedOn, SYSUTCDATETIME())
                WHERE Status = 6
                  AND ManagersNotifiedOfHeldRejectionOn IS NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status_ManagersNotifiedOfHeldRejectionOn",
                table: "Documents",
                columns: new[] { "Status", "ManagersNotifiedOfHeldRejectionOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_Status_ManagersNotifiedOfHeldRejectionOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ManagersNotifiedOfHeldRejectionOn",
                table: "Documents");
        }
    }
}
