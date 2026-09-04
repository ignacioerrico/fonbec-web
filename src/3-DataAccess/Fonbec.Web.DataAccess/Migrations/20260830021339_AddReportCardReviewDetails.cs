using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReportCardReviewDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Absences",
                table: "ReportCardReviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedPeriodMatches",
                table: "ReportCardReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "OverallAssessment",
                table: "ReportCardReviews",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.InsertData(
                table: "RejectedReasons",
                columns: new[] { "Id", "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { 11, null, "Other", "Otro", true });

            migrationBuilder.Sql(
                "UPDATE [Documents] SET [RejectedReasonId] = 11 WHERE [RejectedReasonId] = 10");

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { (byte)2, "WrongPeriod", "Período incorrecto", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Absences",
                table: "ReportCardReviews");

            migrationBuilder.DropColumn(
                name: "ConfirmedPeriodMatches",
                table: "ReportCardReviews");

            migrationBuilder.DropColumn(
                name: "OverallAssessment",
                table: "ReportCardReviews");

            migrationBuilder.Sql(
                "UPDATE [Documents] SET [RejectedReasonId] = 10 WHERE [RejectedReasonId] = 11");

            migrationBuilder.DeleteData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "RejectedReasons",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[] { null, "Other", "Otro", true });
        }
    }
}
