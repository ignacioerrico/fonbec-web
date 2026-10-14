using System.Text;
using ClosedXML.Excel;

namespace Fonbec.Web.Logic.Import;

public static class ChapterImportTemplates
{
    public static byte[] Excel()
    {
        using var workbook = new XLWorkbook();
        AddSheet(workbook, ChapterImportParser.StudentsSheet, ChapterImportColumns.Students);
        AddSheet(workbook, ChapterImportParser.SponsorsSheet, ChapterImportColumns.Sponsors);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] StudentsCsv() => Csv(ChapterImportColumns.Students);

    public static byte[] SponsorsCsv() => Csv(ChapterImportColumns.Sponsors);

    private static void AddSheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers)
    {
        var sheet = workbook.AddWorksheet(name);
        for (var index = 0; index < headers.Count; index++)
        {
            var cell = sheet.Cell(1, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            sheet.Column(index + 1).Width = Math.Max(headers[index].Length + 2, 14);
        }

        sheet.SheetView.FreezeRows(1);
    }

    private static byte[] Csv(IReadOnlyList<string> headers)
    {
        var line = string.Join(';', headers) + "\r\n";
        var body = Encoding.UTF8.GetBytes(line);
        var bom = Encoding.UTF8.GetPreamble();
        var content = new byte[bom.Length + body.Length];
        bom.CopyTo(content, 0);
        body.CopyTo(content, bom.Length);
        return content;
    }
}