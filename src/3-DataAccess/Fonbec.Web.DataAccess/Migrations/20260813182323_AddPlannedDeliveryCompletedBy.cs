using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedDeliveryCompletedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedById",
                table: "PlannedDeliveries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOnUtc",
                table: "PlannedDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedDeliveries_CompletedById",
                table: "PlannedDeliveries",
                column: "CompletedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedDeliveries_AspNetUsers_CompletedById",
                table: "PlannedDeliveries",
                column: "CompletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedDeliveries_AspNetUsers_CompletedById",
                table: "PlannedDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_PlannedDeliveries_CompletedById",
                table: "PlannedDeliveries");

            migrationBuilder.DropColumn(
                name: "CompletedById",
                table: "PlannedDeliveries");

            migrationBuilder.DropColumn(
                name: "CompletedOnUtc",
                table: "PlannedDeliveries");
        }
    }
}
