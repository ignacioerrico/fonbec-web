using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AuditableEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "Chapters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DisabledById",
                table: "Chapters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledOnUtc",
                table: "Chapters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedById",
                table: "Chapters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedOnUtc",
                table: "Chapters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReenabledById",
                table: "Chapters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReenabledOnUtc",
                table: "Chapters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "DisabledById",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledOnUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedById",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedOnUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReenabledById",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReenabledOnUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CreatedById",
                table: "Chapters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_DisabledById",
                table: "Chapters",
                column: "DisabledById");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_LastUpdatedById",
                table: "Chapters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ReenabledById",
                table: "Chapters",
                column: "ReenabledById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ChapterId",
                table: "AspNetUsers",
                column: "ChapterId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_CreatedById",
                table: "AspNetUsers",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DisabledById",
                table: "AspNetUsers",
                column: "DisabledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers",
                column: "LastUpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ReenabledById",
                table: "AspNetUsers",
                column: "ReenabledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Chapters_ChapterId",
                table: "AspNetUsers",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_AspNetUsers_CreatedById",
                table: "Chapters",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_AspNetUsers_DisabledById",
                table: "Chapters",
                column: "DisabledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_AspNetUsers_LastUpdatedById",
                table: "Chapters",
                column: "LastUpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_AspNetUsers_ReenabledById",
                table: "Chapters",
                column: "ReenabledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_CreatedById",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DisabledById",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_LastUpdatedById",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ReenabledById",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Chapters_ChapterId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_AspNetUsers_CreatedById",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_AspNetUsers_DisabledById",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_AspNetUsers_LastUpdatedById",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_AspNetUsers_ReenabledById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_CreatedById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_DisabledById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_LastUpdatedById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_ReenabledById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ChapterId",
                table: "AspNetUsers");

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

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "DisabledById",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "DisabledOnUtc",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "LastUpdatedOnUtc",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ReenabledById",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ReenabledOnUtc",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisabledById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisabledOnUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastUpdatedOnUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReenabledById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReenabledOnUtc",
                table: "AspNetUsers");
        }
    }
}
