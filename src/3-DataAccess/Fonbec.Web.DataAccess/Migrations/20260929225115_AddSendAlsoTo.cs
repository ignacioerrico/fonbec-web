using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSendAlsoTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SendAlsoTos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientFirstName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecipientLastName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SendAsBcc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SponsorId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SendAlsoTos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendAlsoTos_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SendAlsoTos_AspNetUsers_DisabledById",
                        column: x => x.DisabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SendAlsoTos_AspNetUsers_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SendAlsoTos_AspNetUsers_ReenabledById",
                        column: x => x.ReenabledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SendAlsoTos_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SendAlsoTos_CreatedById",
                table: "SendAlsoTos",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SendAlsoTos_DisabledById",
                table: "SendAlsoTos",
                column: "DisabledById");

            migrationBuilder.CreateIndex(
                name: "IX_SendAlsoTos_LastUpdatedById",
                table: "SendAlsoTos",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SendAlsoTos_ReenabledById",
                table: "SendAlsoTos",
                column: "ReenabledById");

            migrationBuilder.CreateIndex(
                name: "IX_SendAlsoTos_SponsorId_RecipientEmail",
                table: "SendAlsoTos",
                columns: new[] { "SponsorId", "RecipientEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SendAlsoTos");
        }
    }
}
