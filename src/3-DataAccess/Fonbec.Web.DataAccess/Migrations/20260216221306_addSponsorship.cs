using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addSponsorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sponsorships",
                columns: table => new
                {
                    SponsorshipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Sponsorships", x => x.SponsorshipId);
                    table.ForeignKey(
                        name: "FK_Sponsorships_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sponsorships_AspNetUsers_DisabledById",
                        column: x => x.DisabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sponsorships_AspNetUsers_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sponsorships_AspNetUsers_ReenabledById",
                        column: x => x.ReenabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sponsorships_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sponsorships_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_CreatedById",
                table: "Sponsorships",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_DisabledById",
                table: "Sponsorships",
                column: "DisabledById");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_LastUpdatedById",
                table: "Sponsorships",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_ReenabledById",
                table: "Sponsorships",
                column: "ReenabledById");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_SponsorId",
                table: "Sponsorships",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_StudentId",
                table: "Sponsorships",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sponsorships");
        }
    }
}
