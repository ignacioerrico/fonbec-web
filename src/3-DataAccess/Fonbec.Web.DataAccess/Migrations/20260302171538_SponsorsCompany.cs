using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SponsorsCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Sponsors",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_CompanyId",
                table: "Sponsors",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sponsors_Companies_CompanyId",
                table: "Sponsors",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sponsors_Companies_CompanyId",
                table: "Sponsors");

            migrationBuilder.DropIndex(
                name: "IX_Sponsors_CompanyId",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Sponsors");
        }
    }
}
