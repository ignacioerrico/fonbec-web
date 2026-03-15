using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyToSponsorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SponsorId",
                table: "Sponsorships",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Sponsorships",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_CompanyId",
                table: "Sponsorships",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sponsorships_Companies_CompanyId",
                table: "Sponsorships",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sponsorships_Companies_CompanyId",
                table: "Sponsorships");

            migrationBuilder.DropIndex(
                name: "IX_Sponsorships_CompanyId",
                table: "Sponsorships");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Sponsorships");

            migrationBuilder.AlterColumn<int>(
                name: "SponsorId",
                table: "Sponsorships",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
