using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSendAlsoToNamesWithFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "SendAlsoTos",
                type: "nvarchar(81)",
                maxLength: 81,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE SendAlsoTos
                SET RecipientName = LEFT(LTRIM(RTRIM(RecipientFirstName + N' ' + RecipientLastName)), 81)
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientName",
                table: "SendAlsoTos",
                type: "nvarchar(81)",
                maxLength: 81,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(81)",
                oldMaxLength: 81,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "RecipientFirstName",
                table: "SendAlsoTos");

            migrationBuilder.DropColumn(
                name: "RecipientLastName",
                table: "SendAlsoTos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecipientFirstName",
                table: "SendAlsoTos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientLastName",
                table: "SendAlsoTos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE SendAlsoTos
                SET RecipientFirstName = LEFT(
                        CASE
                            WHEN CHARINDEX(N' ', RecipientName) = 0 THEN RecipientName
                            ELSE LEFT(RecipientName, CHARINDEX(N' ', RecipientName) - 1)
                        END, 40),
                    RecipientLastName = LEFT(
                        CASE
                            WHEN CHARINDEX(N' ', RecipientName) = 0 THEN N''
                            ELSE LTRIM(SUBSTRING(RecipientName, CHARINDEX(N' ', RecipientName) + 1, 81))
                        END, 40)
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientFirstName",
                table: "SendAlsoTos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientLastName",
                table: "SendAlsoTos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "SendAlsoTos");
        }
    }
}
