using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterDeliveryStatisticsAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionImprovementLockedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionReviewLockedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImprovementCompletedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImprovementFirstLockedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedById",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewFirstLockedOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RejectedById",
                table: "Documents",
                column: "RejectedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AspNetUsers_RejectedById",
                table: "Documents",
                column: "RejectedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_RejectedById",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_RejectedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DecisionImprovementLockedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DecisionReviewLockedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ImprovementCompletedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ImprovementFirstLockedOn",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RejectedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewFirstLockedOn",
                table: "Documents");
        }
    }
}
