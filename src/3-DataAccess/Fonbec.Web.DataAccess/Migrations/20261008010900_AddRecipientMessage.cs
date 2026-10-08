using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipientMessages",
                columns: table => new
                {
                    RecipientMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SentOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SharedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SharedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipientMessages", x => x.RecipientMessageId);
                    table.CheckConstraint("CK_RecipientMessage_RecipientRequired", "([SponsorId] IS NOT NULL AND [CompanyId] IS NULL) OR ([SponsorId] IS NULL AND [CompanyId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RecipientMessages_AspNetUsers_SharedById",
                        column: x => x.SharedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipientMessages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipientMessages_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipientMessages_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipientMessages_CompanyId_StudentId_SentOn",
                table: "RecipientMessages",
                columns: new[] { "CompanyId", "StudentId", "SentOn" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipientMessages_SharedById",
                table: "RecipientMessages",
                column: "SharedById");

            migrationBuilder.CreateIndex(
                name: "IX_RecipientMessages_SponsorId_StudentId_SentOn",
                table: "RecipientMessages",
                columns: new[] { "SponsorId", "StudentId", "SentOn" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipientMessages_StudentId",
                table: "RecipientMessages",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipientMessages");
        }
    }
}
