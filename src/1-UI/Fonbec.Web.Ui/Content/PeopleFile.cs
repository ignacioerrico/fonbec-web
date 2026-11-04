using System.Text;

namespace Fonbec.Web.Ui.Content;

public sealed record Person(string Name, string Role, string? Detail);

public static class PeopleFile
{
    public static IReadOnlyList<Person> Load(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "Content", "people.csv");
        if (!File.Exists(path))
        {
            return [];
        }

        return Read(File.ReadAllText(path));
    }

    public static IReadOnlyList<Person> Read(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        var rows = ParseRows(csv.TrimStart('\uFEFF'));
        if (rows.Count == 0)
        {
            return [];
        }

        var header = rows[0];
        var nameIndex = IndexOf(header, "Nombre");
        var roleIndex = IndexOf(header, "Rol");
        var detailIndex = IndexOf(header, "Detalle");
        if (nameIndex < 0 || roleIndex < 0)
        {
            return [];
        }

        var people = new List<Person>();
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var name = Field(row, nameIndex);
            var role = Field(row, roleIndex);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            var detail = detailIndex < 0 ? null : Field(row, detailIndex);
            people.Add(new Person(name, role, string.IsNullOrWhiteSpace(detail) ? null : detail));
        }

        return people;
    }

    private static int IndexOf(IReadOnlyList<string> header, string name)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Field(IReadOnlyList<string> row, int index) =>
        index < row.Count ? row[index] : string.Empty;

    private static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var current = csv[i];
            if (inQuotes)
            {
                if (current == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                field.Append(current);
                continue;
            }

            switch (current)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    row.Add(field.ToString().Trim());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    FinishRow(rows, row, field);
                    row = [];
                    break;
                default:
                    field.Append(current);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            FinishRow(rows, row, field);
        }

        return rows;
    }

    private static void FinishRow(List<List<string>> rows, List<string> row, StringBuilder field)
    {
        row.Add(field.ToString().Trim());
        field.Clear();
        if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            rows.Add(row);
        }
    }
}