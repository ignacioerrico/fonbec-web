using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Fonbec.Web.Logic.Import;

namespace Fonbec.Web.Logic.Tests.Import;

public class ChapterImportParserTests
{
    [Fact]
    public void ExcelTemplate_Parses_With_Both_Sheets_And_No_Rows()
    {
        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("plantilla.xlsx", ChapterImportTemplates.Excel(), ChapterImportUploadKind.Excel),
        ]);

        parsed.FileErrors.Should().BeEmpty();
        parsed.Students.Should().BeEmpty();
        parsed.Sponsors.Should().BeEmpty();
    }

    [Fact]
    public void CsvTemplates_Parse_With_No_Rows()
    {
        var students = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("becarios.csv", ChapterImportTemplates.StudentsCsv(), ChapterImportUploadKind.StudentsCsv),
        ]);
        var sponsors = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("padrinos.csv", ChapterImportTemplates.SponsorsCsv(), ChapterImportUploadKind.SponsorsCsv),
        ]);

        students.FileErrors.Should().BeEmpty();
        students.Students.Should().BeEmpty();
        sponsors.FileErrors.Should().BeEmpty();
        sponsors.Sponsors.Should().BeEmpty();
    }

    [Fact]
    public void Excel_Reads_Both_Sheets_And_Skips_Blank_Rows()
    {
        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("filial.xlsx", WorkbookWithBlankRow(), ChapterImportUploadKind.Excel),
        ]);

        parsed.FileErrors.Should().BeEmpty();
        parsed.Students.Should().ContainSingle();
        parsed.Students[0].FirstName.Should().Be("Ana");
        parsed.Students[0].LastName.Should().Be("Pérez");
        parsed.Students[0].SecondaryYear.Should().Be("2020");
        parsed.Students[0].RowNumber.Should().Be(2);
        parsed.Sponsors.Should().ContainSingle();
        parsed.Sponsors[0].Email.Should().Be("ana@ejemplo.com");
    }

    [Fact]
    public void Csv_Accepts_Semicolon_And_Comma()
    {
        var semicolon = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("becarios.csv", Utf8("Nombre;Apellido\nAna;Pérez\n"), ChapterImportUploadKind.StudentsCsv),
        ]);
        var comma = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("padrinos.csv", Utf8("Nombre,Apellido,Email\nLuis,Díaz,luis@ejemplo.com\n"), ChapterImportUploadKind.SponsorsCsv),
        ]);

        semicolon.FileErrors.Should().BeEmpty();
        semicolon.Students.Should().ContainSingle();
        semicolon.Students[0].LastName.Should().Be("Pérez");
        comma.FileErrors.Should().BeEmpty();
        comma.Sponsors.Should().ContainSingle();
        comma.Sponsors[0].Email.Should().Be("luis@ejemplo.com");
    }

    [Fact]
    public void Csv_Skips_Blank_Rows_And_Keeps_File_Row_Numbers()
    {
        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload(
                "becarios.csv",
                Utf8("Nombre;Apellido\nAna;Pérez\n;\nLuis;Gómez\n"),
                ChapterImportUploadKind.StudentsCsv),
        ]);

        parsed.FileErrors.Should().BeEmpty();
        parsed.Students.Select(row => row.RowNumber).Should().Equal(2, 4);
    }

    [Fact]
    public void Csv_Matches_Headers_Without_Accents()
    {
        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload(
                "becarios.csv",
                Utf8("Nombre;Apellido;Genero;Telefono\nAna;Pérez;Femenino;1155555555\n"),
                ChapterImportUploadKind.StudentsCsv),
        ]);

        parsed.FileErrors.Should().BeEmpty();
        parsed.Students.Should().ContainSingle();
        parsed.Students[0].Gender.Should().Be("Femenino");
        parsed.Students[0].Phone.Should().Be("1155555555");
    }

    [Fact]
    public void Csv_Decodes_Windows1252_When_Utf8_Is_Invalid()
    {
        var content = Encoding.ASCII.GetBytes("Nombre;Apellido\nMar")
            .Concat(new byte[] { 0xED })
            .Concat(Encoding.ASCII.GetBytes("a;L"))
            .Concat(new byte[] { 0xF3 })
            .Concat(Encoding.ASCII.GetBytes("pez\n"))
            .ToArray();

        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("becarios.csv", content, ChapterImportUploadKind.StudentsCsv),
        ]);

        parsed.FileErrors.Should().BeEmpty();
        parsed.Students.Should().ContainSingle();
        parsed.Students[0].FirstName.Should().Be("María");
        parsed.Students[0].LastName.Should().Be("López");
    }

    [Fact]
    public void Missing_Required_Columns_Are_A_File_Error()
    {
        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("becarios.csv", Utf8("Apodo\nLuchi\n"), ChapterImportUploadKind.StudentsCsv),
            new ChapterImportUpload("padrinos.csv", Utf8("Nombre;Apellido\nAna;Pérez\n"), ChapterImportUploadKind.SponsorsCsv),
        ]);

        parsed.FileErrors.Should().Contain(error => error.Contains("Becarios") && error.Contains("Nombre"));
        parsed.FileErrors.Should().Contain(error => error.Contains("Padrinos") && error.Contains("Email"));
        parsed.Students.Should().BeEmpty();
        parsed.Sponsors.Should().BeEmpty();
    }

    [Fact]
    public void Excel_Without_Known_Sheets_Is_A_File_Error()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Otra");
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var parsed = ChapterImportParser.Parse(
        [
            new ChapterImportUpload("filial.xlsx", stream.ToArray(), ChapterImportUploadKind.Excel),
        ]);

        parsed.FileErrors.Should().ContainSingle();
        parsed.FileErrors[0].Should().Contain("Becarios");
    }

    private static byte[] WorkbookWithBlankRow()
    {
        using var workbook = new XLWorkbook();
        var students = workbook.AddWorksheet("Becarios");
        students.Cell(1, 1).Value = "Nombre";
        students.Cell(1, 2).Value = "Apellido";
        students.Cell(1, 3).Value = "Inicio secundaria";
        students.Cell(2, 1).Value = "Ana";
        students.Cell(2, 2).Value = "Pérez";
        students.Cell(2, 3).Value = 2020;
        students.Cell(3, 1).Value = " ";

        var sponsors = workbook.AddWorksheet("Padrinos");
        sponsors.Cell(1, 1).Value = "Nombre";
        sponsors.Cell(1, 2).Value = "Apellido";
        sponsors.Cell(1, 3).Value = "Email";
        sponsors.Cell(2, 1).Value = "Ana";
        sponsors.Cell(2, 2).Value = "Pérez";
        sponsors.Cell(2, 3).Value = "ana@ejemplo.com";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}