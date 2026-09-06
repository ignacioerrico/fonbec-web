using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterFollowUpResolutionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GreenFlagResolvedById",
                table: "Assessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GreenFlagResolvedOn",
                table: "Assessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGreenFlagResolved",
                table: "Assessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRedFlagResolved",
                table: "Assessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RedFlagResolvedById",
                table: "Assessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RedFlagResolvedOn",
                table: "Assessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_GreenFlagResolvedById",
                table: "Assessments",
                column: "GreenFlagResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_RedFlagResolvedById",
                table: "Assessments",
                column: "RedFlagResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_AspNetUsers_GreenFlagResolvedById",
                table: "Assessments",
                column: "GreenFlagResolvedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_AspNetUsers_RedFlagResolvedById",
                table: "Assessments",
                column: "RedFlagResolvedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_AspNetUsers_GreenFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_AspNetUsers_RedFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_GreenFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_RedFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "GreenFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "GreenFlagResolvedOn",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "IsGreenFlagResolved",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "IsRedFlagResolved",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "RedFlagResolvedById",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "RedFlagResolvedOn",
                table: "Assessments");
        }
    }
}
