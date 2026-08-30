using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenumberRejectedReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "Description" },
                values: new object[] { "MissingWrittenDate", "No figura la fecha" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Description" },
                values: new object[] { "MissingAddressee", "No figura el destinatario" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Description" },
                values: new object[] { "MissingAuthor", "No figura el firmante" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Description" },
                values: new object[] { "NotALetter", "No es una carta" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongAddressee", "Destinatario incorrecto" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongSigner", "Firmante incorrecto" });

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
                columns: new[] { "Code", "Description" },
                values: new object[] { "Unreadable", "No legible" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "Description" },
                values: new object[] { "NotALetter", "No es una carta" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongAddressee", "Destinatario incorrecto" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongSigner", "Firma incorrecta" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Description" },
                values: new object[] { "Illegible", "Ilegible" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Code", "Description" },
                values: new object[] { "InappropriateContent", "Contenido inapropiado" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongDate", "Fecha incorrecta" });

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
                values: new object[] { (byte)3, "Unreadable", "No legible" });

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Code", "Description" },
                values: new object[] { "WrongDocument", "Documento incorrecto" });

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
    }
}
