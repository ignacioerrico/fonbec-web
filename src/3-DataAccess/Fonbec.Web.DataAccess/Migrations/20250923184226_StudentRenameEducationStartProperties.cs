using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StudentRenameEducationStartProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniversitySinceUtc",
                table: "Students",
                newName: "UniversityStartYear");

            migrationBuilder.RenameColumn(
                name: "SecondarySchoolSinceUtc",
                table: "Students",
                newName: "SecondarySchoolStartYear");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniversityStartYear",
                table: "Students",
                newName: "UniversitySinceUtc");

            migrationBuilder.RenameColumn(
                name: "SecondarySchoolStartYear",
                table: "Students",
                newName: "SecondarySchoolSinceUtc");
        }
    }
}
