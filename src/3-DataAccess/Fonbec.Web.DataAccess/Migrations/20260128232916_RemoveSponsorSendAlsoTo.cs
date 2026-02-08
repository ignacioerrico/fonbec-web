using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSponsorSendAlsoTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchOffice",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "SendAlsoTo",
                table: "Sponsors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchOffice",
                table: "Sponsors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SendAlsoTo",
                table: "Sponsors",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
