using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace Fonbec.Web.Logic.Import;

public static partial class ChapterImportParser
{
    public const string StudentsSheet = "Becarios";
    public const string SponsorsSheet = "Padrinos";

    private static Encoding? windows1252;

    static ChapterImportParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static Encoding Windows1252 => windows1252 ??= Encoding.GetEncoding(1252);

    public static ChapterImportParseResult Parse(IReadOnlyList<ChapterImportUpload> files)
    {
        var result = new ChapterImportParseResult();
        if (files.Count == 0)
        {
            result.FileErrors.Add("Elegí un archivo para importar.");
            return result;
        }

        foreach (var file in files)
        {
            if (file.Content.Length == 0)
            {
                result.FileErrors.Add($"{DisplayName(file)} está vacío.");
                continue;
            }

            if (file.Content.Length > ChapterImportLimits.MaxFileBytes)
            {
                result.FileErrors.Add($"{DisplayName(file)} supera el tamaño máximo (2 MB).");
                continue;
            }

            try
            {
                if (file.Kind == ChapterImportUploadKind.Excel)
                {
                    ParseExcel(file, result);
                }
                else
                {
                    ParseCsv(file, result);
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                var kind = file.Kind == ChapterImportUploadKind.Excel ? "Excel" : "CSV";
                result.FileErrors.Add($"No se pudo leer el archivo {kind} {DisplayName(file)}.");
            }
        }

        return result;
    }

    private static void ParseExcel(ChapterImportUpload file, ChapterImportParseResult result)
    {
        using var stream = new MemoryStream(file.Content, writable: false);
        using var workbook = new XLWorkbook(stream);

        var matched = false;
        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetKey = NormalizeHeader(worksheet.Name);
            var isStudents = sheetKey == NormalizeHeader(StudentsSheet);
            var isSponsors = sheetKey == NormalizeHeader(SponsorsSheet);
            if (!isStudents && !isSponsors)
            {
                continue;
            }

            matched = true;
            var sheetName = isStudents ? StudentsSheet : SponsorsSheet;
            var rows = isStudents ? result.Students : result.Sponsors;
            ParseWorksheet(worksheet, sheetName, rows, result.FileErrors, requireEmail: isSponsors);
        }

        if (!matched)
        {
            result.FileErrors.Add($"{DisplayName(file)} no tiene las hojas {StudentsSheet} ni {SponsorsSheet}.");
        }
    }

    private static void ParseWorksheet(
        IXLWorksheet worksheet,
        string sheetName,
        List<ChapterImportParsedRow> rows,
        List<string> fileErrors,
        bool requireEmail)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow == 0 || lastColumn == 0)
        {
            fileErrors.Add($"La hoja {sheetName} no tiene encabezados.");
            return;
        }

        var columns = ReadHeader(column => CellText(worksheet.Cell(1, column)), lastColumn);
        if (!HasRequiredColumns(columns, requireEmail, sheetName, fileErrors))
        {
            return;
        }

        var dataRows = 0;
        var firstAdded = rows.Count;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var values = ReadRow(columns, column => CellText(worksheet.Cell(rowNumber, column)));
            if (IsBlank(values))
            {
                continue;
            }

            dataRows++;
            if (dataRows > ChapterImportLimits.MaxRows)
            {
                rows.RemoveRange(firstAdded, rows.Count - firstAdded);
                fileErrors.Add($"La hoja {sheetName} tiene más de {ChapterImportLimits.MaxRows} filas.");
                return;
            }

            rows.Add(ToParsedRow(sheetName, rowNumber, values));
        }
    }

    private static void ParseCsv(ChapterImportUpload file, ChapterImportParseResult result)
    {
        var isStudents = file.Kind == ChapterImportUploadKind.StudentsCsv;
        var sheetName = isStudents ? StudentsSheet : SponsorsSheet;
        var text = Decode(file.Content);
        if (string.IsNullOrWhiteSpace(text))
        {
            result.FileErrors.Add($"{DisplayName(file)} está vacío.");
            return;
        }

        var firstLine = FirstLine(text);
        if (string.IsNullOrWhiteSpace(firstLine))
        {
            result.FileErrors.Add($"{DisplayName(file)} no tiene encabezados.");
            return;
        }

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DetectDelimiter(firstLine),
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = false,
        };

        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, configuration);
        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            result.FileErrors.Add($"{DisplayName(file)} no tiene encabezados.");
            return;
        }

        var columns = ReadHeader(index => csv.HeaderRecord.ElementAtOrDefault(index - 1) ?? string.Empty, csv.HeaderRecord.Length);
        if (!HasRequiredColumns(columns, requireEmail: !isStudents, sheetName, result.FileErrors))
        {
            return;
        }

        var rows = isStudents ? result.Students : result.Sponsors;
        var dataRows = 0;
        var firstAdded = rows.Count;
        while (csv.Read())
        {
            var values = ReadRow(columns, column => csv.GetField(column - 1) ?? string.Empty);
            if (IsBlank(values))
            {
                continue;
            }

            dataRows++;
            if (dataRows > ChapterImportLimits.MaxRows)
            {
                rows.RemoveRange(firstAdded, rows.Count - firstAdded);
                result.FileErrors.Add($"{sheetName} tiene más de {ChapterImportLimits.MaxRows} filas.");
                return;
            }

            rows.Add(ToParsedRow(sheetName, csv.Context.Parser?.RawRow ?? dataRows + 1, values));
        }
    }

    private static Dictionary<string, int> ReadHeader(Func<int, string> cellAt, int columnCount)
    {
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var column = 1; column <= columnCount; column++)
        {
            var key = HeaderKey(cellAt(column));
            if (key.Length > 0 && !columns.ContainsKey(key))
            {
                columns.Add(key, column);
            }
        }

        return columns;
    }

    private static Dictionary<string, string> ReadRow(Dictionary<string, int> columns, Func<int, string> cellAt)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, column) in columns)
        {
            values[key] = cellAt(column).Trim();
        }

        return values;
    }

    private static bool HasRequiredColumns(
        Dictionary<string, int> columns,
        bool requireEmail,
        string sheetName,
        List<string> fileErrors)
    {
        var missing = new List<string>();
        if (!columns.ContainsKey(HeaderKey(ChapterImportColumns.FirstName)))
        {
            missing.Add(ChapterImportColumns.FirstName);
        }

        if (!columns.ContainsKey(HeaderKey(ChapterImportColumns.LastName)))
        {
            missing.Add(ChapterImportColumns.LastName);
        }

        if (requireEmail && !columns.ContainsKey(HeaderKey(ChapterImportColumns.Email)))
        {
            missing.Add(ChapterImportColumns.Email);
        }

        if (missing.Count == 0)
        {
            return true;
        }

        fileErrors.Add($"Faltan columnas en {sheetName}: {string.Join(", ", missing)}.");
        return false;
    }

    private static ChapterImportParsedRow ToParsedRow(string sheetName, int rowNumber, Dictionary<string, string> values) =>
        new()
        {
            Sheet = sheetName,
            RowNumber = rowNumber,
            FirstName = Value(values, ChapterImportColumns.FirstName),
            LastName = Value(values, ChapterImportColumns.LastName),
            NickName = Value(values, ChapterImportColumns.NickName),
            Gender = Value(values, ChapterImportColumns.Gender),
            Email = Value(values, ChapterImportColumns.Email, "correo", "correo electronico"),
            Phone = Value(values, ChapterImportColumns.Phone),
            Notes = Value(values, ChapterImportColumns.Notes, "informacion adicional"),
            SecondaryYear = Value(values, ChapterImportColumns.SecondaryYear),
            UniversityYear = Value(values, ChapterImportColumns.UniversityYear),
            FacilitatorEmail = Value(values, ChapterImportColumns.Facilitator),
        };

    private static string Value(Dictionary<string, string> values, string header, params string[] aliases)
    {
        if (values.TryGetValue(HeaderKey(header), out var value))
        {
            return value;
        }

        foreach (var alias in aliases)
        {
            if (values.TryGetValue(alias, out value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static bool IsBlank(Dictionary<string, string> values) =>
        values.Values.All(string.IsNullOrWhiteSpace);

    internal static string HeaderKey(string header)
    {
        var collapsed = Whitespace().Replace(header.Trim(), " ");
        var decomposed = collapsed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static string NormalizeHeader(string value) => HeaderKey(value);

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.DataType switch
        {
            XLDataType.Number => FormatNumber(cell.GetDouble()),
            XLDataType.DateTime => cell.GetDateTime().Year.ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            _ => cell.GetString().Trim(),
        };
    }

    private static string FormatNumber(double value)
    {
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) < 0.0001)
        {
            return rounded.ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Decode(byte[] content)
    {
        string text;
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
        {
            text = Encoding.UTF8.GetString(content);
        }
        else
        {
            try
            {
                var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                text = utf8.GetString(content);
            }
            catch (DecoderFallbackException)
            {
                text = Windows1252.GetString(content);
            }
        }

        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }

    private static string FirstLine(string text)
    {
        var end = text.IndexOfAny(['\r', '\n']);
        return end < 0 ? text : text[..end];
    }

    private static string DetectDelimiter(string headerLine)
    {
        var semicolons = headerLine.Count(character => character == ';');
        var commas = headerLine.Count(character => character == ',');
        return semicolons > commas ? ";" : ",";
    }

    private static string DisplayName(ChapterImportUpload file) =>
        string.IsNullOrWhiteSpace(file.FileName) ? "El archivo" : file.FileName;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}