using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRedFlagPriorityAndLetterMissingReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "RedFlagPriority",
                table: "Assessments",
                type: "tinyint",
                nullable: true);

            migrationBuilder.InsertData(
                table: "RejectedReasons",
                columns: new[] { "Id", "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[,]
                {
                    { 13, (byte)1, "MissingWrittenDate", "No figura la fecha", false },
                    { 14, (byte)1, "MissingAddressee", "No figura el destinatario", false },
                    { 15, (byte)1, "MissingAuthor", "No figura el firmante", false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DropColumn(
                name: "RedFlagPriority",
                table: "Assessments");
        }
    }
}
