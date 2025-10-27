using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToAuditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration file has been manually modified from its original to prevent data loss.
            // The original migration would have dropped the Description column, resulting in data loss.

            // 1. Add the Notes column to the Chapters table
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Chapters",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            // 2. Copy data from Description to Notes
            migrationBuilder.Sql(
                @"UPDATE Chapters 
                  SET Notes = Description");

            // 3. Drop the Description column from the Chapters table
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Chapters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. If rolling back, add Description back
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Chapters",
                type: "nvarchar(127)",
                maxLength: 127,
                nullable: true);

            // 2. Copy data from Notes to Description
            migrationBuilder.Sql(
                @"UPDATE Chapters 
                  SET Description = Notes");

            // 3. Drop Notes column
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Chapters");
        }
    }
}
