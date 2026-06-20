using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsOfContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointsOfContact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledById = table.Column<int>(type: "int", nullable: true),
                    DisabledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReenabledById = table.Column<int>(type: "int", nullable: true),
                    ReenabledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsOfContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsOfContact_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointsOfContact_AspNetUsers_DisabledById",
                        column: x => x.DisabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PointsOfContact_AspNetUsers_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PointsOfContact_AspNetUsers_ReenabledById",
                        column: x => x.ReenabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PointsOfContact_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfContact_CompanyId",
                table: "PointsOfContact",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfContact_CreatedById",
                table: "PointsOfContact",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfContact_DisabledById",
                table: "PointsOfContact",
                column: "DisabledById");

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfContact_LastUpdatedById",
                table: "PointsOfContact",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfContact_ReenabledById",
                table: "PointsOfContact",
                column: "ReenabledById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsOfContact");
        }
    }
}
