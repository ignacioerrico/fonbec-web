using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UnifyUnreadableRejectedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AppliesToDocumentType", "Code", "Description" },
                values: new object[] { (byte)2, "NotReportCard", "No es boletín o libreta" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongStudentName", "Nombre del estudiante incorrecto" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "AppliesToDocumentType", "Code", "Description" },
                values: new object[] { null, "Unreadable", "No legible" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { null, "Other", "Otro", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AppliesToDocumentType", "Code", "Description" },
                values: new object[] { (byte)1, "Illegible", "Ilegible" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Code", "Description" },
                values: new object[] { "NotReportCard", "No es boletín o libreta" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "AppliesToDocumentType", "Code", "Description" },
                values: new object[] { (byte)2, "WrongStudentName", "Nombre del estudiante incorrecto" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { (byte)3, "Unreadable", "No legible", false });

            migrationBuilder.InsertData(
                table: "RejectedReasons",
                columns: new[] { "Id", "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { 11, null, "Other", "Otro", true });
        }
    }
}
