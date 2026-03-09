using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PlannedDeliveryCreateRelationshipWithChapter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ChapterId",
                table: "PlannedDeliveries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedDeliveries_ChapterId",
                table: "PlannedDeliveries",
                column: "ChapterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedDeliveries_Chapters_ChapterId",
                table: "PlannedDeliveries",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedDeliveries_Chapters_ChapterId",
                table: "PlannedDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_PlannedDeliveries_ChapterId",
                table: "PlannedDeliveries");

            migrationBuilder.AlterColumn<int>(
                name: "ChapterId",
                table: "PlannedDeliveries",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
