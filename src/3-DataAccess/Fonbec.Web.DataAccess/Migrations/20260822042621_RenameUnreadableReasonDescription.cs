using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameUnreadableReasonDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "Ilegible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "No legible");
        }
    }
}
