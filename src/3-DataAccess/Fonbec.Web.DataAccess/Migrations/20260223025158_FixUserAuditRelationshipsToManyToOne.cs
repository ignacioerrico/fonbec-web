using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixUserAuditRelationshipsToManyToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CreatedById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DisabledById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReenabledById",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedById",
                table: "AspNetUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DisabledById",
                table: "AspNetUsers",
                column: "DisabledById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReenabledById",
                table: "AspNetUsers",
                column: "ReenabledById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CreatedById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DisabledById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReenabledById",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedById",
                table: "AspNetUsers",
                column: "CreatedById",
                unique: true,
                filter: "[CreatedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DisabledById",
                table: "AspNetUsers",
                column: "DisabledById",
                unique: true,
                filter: "[DisabledById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers",
                column: "LastUpdatedById",
                unique: true,
                filter: "[LastUpdatedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReenabledById",
                table: "AspNetUsers",
                column: "ReenabledById",
                unique: true,
                filter: "[ReenabledById] IS NOT NULL");
        }
    }
}
