using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_CreatedById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_DisabledById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_LastUpdatedById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_ReenabledById",
            //    table: "AspNetUsers");

            //migrationBuilder.CreateTable(
            //    name: "Companies",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            //        Email = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
            //        PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
            //        CreatedById = table.Column<int>(type: "int", nullable: false),
            //        CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            //        LastUpdatedById = table.Column<int>(type: "int", nullable: true),
            //        LastUpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        DisabledById = table.Column<int>(type: "int", nullable: true),
            //        DisabledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        ReenabledById = table.Column<int>(type: "int", nullable: true),
            //        ReenabledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        IsActive = table.Column<bool>(type: "bit", nullable: false),
            //        Notes = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Companies", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Companies_AspNetUsers_CreatedById",
            //            column: x => x.CreatedById,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_Companies_AspNetUsers_DisabledById",
            //            column: x => x.DisabledById,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id");
            //        table.ForeignKey(
            //            name: "FK_Companies_AspNetUsers_LastUpdatedById",
            //            column: x => x.LastUpdatedById,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id");
            //        table.ForeignKey(
            //            name: "FK_Companies_AspNetUsers_ReenabledById",
            //            column: x => x.ReenabledById,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id");
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_CreatedById",
            //    table: "AspNetUsers",
            //    column: "CreatedById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_DisabledById",
            //    table: "AspNetUsers",
            //    column: "DisabledById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_LastUpdatedById",
            //    table: "AspNetUsers",
            //    column: "LastUpdatedById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_ReenabledById",
            //    table: "AspNetUsers",
            //    column: "ReenabledById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Companies_CreatedById",
            //    table: "Companies",
            //    column: "CreatedById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Companies_DisabledById",
            //    table: "Companies",
            //    column: "DisabledById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Companies_LastUpdatedById",
            //    table: "Companies",
            //    column: "LastUpdatedById");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Companies_ReenabledById",
            //    table: "Companies",
            //    column: "ReenabledById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "Companies");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_CreatedById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_DisabledById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_LastUpdatedById",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_ReenabledById",
            //    table: "AspNetUsers");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_CreatedById",
            //    table: "AspNetUsers",
            //    column: "CreatedById",
            //    unique: true,
            //    filter: "[CreatedById] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_DisabledById",
            //    table: "AspNetUsers",
            //    column: "DisabledById",
            //    unique: true,
            //    filter: "[DisabledById] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_LastUpdatedById",
            //    table: "AspNetUsers",
            //    column: "LastUpdatedById",
            //    unique: true,
            //    filter: "[LastUpdatedById] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_AspNetUsers_ReenabledById",
            //    table: "AspNetUsers",
            //    column: "ReenabledById",
            //    unique: true,
            //    filter: "[ReenabledById] IS NOT NULL");
        }
    }
}
