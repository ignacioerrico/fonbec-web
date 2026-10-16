using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Fonbec.Web.Logic.Email;

public static class EmailTemplateRenderer
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-AR");

    private static readonly HashSet<string> AlwaysCapitalized = new(StringComparer.OrdinalIgnoreCase)
    {
        "padrino-apodo",
        "padrino-nombre",
        "padrino-apellido",
        "becario-apodo",
        "becario-nombre",
        "becario-apellido",
    };

    public const string DefaultSubjectPrefix = "FONBEC |";

    public static string ApplySubjectPrefix(string? prefix, string subject)
    {
        var cleanPrefix = StripLineBreaks(prefix).Trim();
        var cleanSubject = StripLineBreaks(subject).Trim();
        if (cleanPrefix.Length == 0)
        {
            return cleanSubject;
        }

        if (cleanSubject.Length == 0)
        {
            return cleanPrefix;
        }

        return $"{cleanPrefix} {cleanSubject}";
    }

    public static string RenderSubject(string template, IReadOnlyDictionary<string, string?> values) =>
        StripLineBreaks(Render(template, values, html: false)).Trim();

    public static string RenderBody(string template, IReadOnlyDictionary<string, string?> values)
    {
        var rendered = Render(template, values, html: true).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var paragraphs = Regex.Split(rendered, @"\n{2,}")
            .Select(paragraph => paragraph.Trim('\n'))
            .Where(paragraph => paragraph.Length > 0)
            .Select(paragraph => $"<p>{paragraph.Replace("\n", "<br>", StringComparison.Ordinal)}</p>");
        return string.Concat(paragraphs);
    }

    private static string Render(string template, IReadOnlyDictionary<string, string?> values, bool html)
    {
        var lookup = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        var builder = new StringBuilder();
        for (var index = 0; index < template.Length;)
        {
            if (TryReadLink(template, index, out var linkText, out var token, out var next))
            {
                AppendLink(builder, linkText, token, lookup, html);
                index = next;
                continue;
            }

            if (TryReadToken(template, index, out token, out next))
            {
                AppendValue(builder, token, lookup, html);
                index = next;
                continue;
            }

            var start = index;
            while (index < template.Length && template[index] is not '[' and not '{')
            {
                index++;
            }

            AppendLiteral(builder, template[start..index], html);
        }

        return builder.ToString();
    }

    private static void AppendLink(
        StringBuilder builder,
        string linkText,
        string token,
        Dictionary<string, string?> values,
        bool html)
    {
        var url = Resolve(token, values) ?? string.Empty;
        if (!html)
        {
            builder.Append(linkText);
            if (url.Length > 0)
            {
                builder.Append(' ');
                builder.Append(url);
            }

            return;
        }

        builder.Append("<a href=\"");
        builder.Append(EncodeHtml(url));
        builder.Append("\">");
        builder.Append(EncodeHtml(linkText));
        builder.Append("</a>");
    }

    private static void AppendValue(
        StringBuilder builder,
        string token,
        Dictionary<string, string?> values,
        bool html)
    {
        if (!values.ContainsKey(token.Trim()))
        {
            AppendLiteral(builder, $"{{{token}}}", html);
            return;
        }

        var value = Resolve(token, values) ?? string.Empty;
        if (!html)
        {
            builder.Append(StripLineBreaks(value));
            return;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        builder.Append(
            EncodePreservingNewLines(normalized).Replace("\n", "<br>", StringComparison.Ordinal));
    }

    private static string? Resolve(string token, Dictionary<string, string?> values)
    {
        var key = token.Trim();
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return value;
        }

        var capitalize = AlwaysCapitalized.Contains(key)
            || (key.Length > 0 && char.IsUpper(key[0]));
        if (!capitalize || value.Length == 0)
        {
            return value;
        }

        return char.ToUpper(value[0], Spanish) + value[1..];
    }

    private static void AppendLiteral(StringBuilder builder, string text, bool html) =>
        builder.Append(html ? EncodePreservingNewLines(text) : text);

    private static string EncodePreservingNewLines(string text)
    {
        var encoded = new StringBuilder();
        var line = new StringBuilder();
        foreach (var character in text)
        {
            if (character is '\r' or '\n')
            {
                encoded.Append(EncodeHtml(line.ToString()));
                line.Clear();
                encoded.Append(character);
                continue;
            }

            line.Append(character);
        }

        encoded.Append(EncodeHtml(line.ToString()));
        return encoded.ToString();
    }

    private static string EncodeHtml(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static bool TryReadLink(
        string template,
        int start,
        out string linkText,
        out string token,
        out int next)
    {
        linkText = string.Empty;
        token = string.Empty;
        next = start;
        if (template[start] != '[')
        {
            return false;
        }

        var closeBracket = template.IndexOf(']', start + 1);
        if (closeBracket < 0 || closeBracket + 1 >= template.Length || template[closeBracket + 1] != '{')
        {
            return false;
        }

        if (template.IndexOfAny(['\r', '\n'], start, closeBracket - start) >= 0)
        {
            return false;
        }

        var closeBrace = template.IndexOf('}', closeBracket + 2);
        if (closeBrace < 0 || template.IndexOfAny(['\r', '\n'], closeBracket, closeBrace - closeBracket) >= 0)
        {
            return false;
        }

        linkText = template[(start + 1)..closeBracket];
        token = template[(closeBracket + 2)..closeBrace];
        next = closeBrace + 1;
        return true;
    }

    private static bool TryReadToken(string template, int start, out string token, out int next)
    {
        token = string.Empty;
        next = start;
        if (template[start] != '{')
        {
            return false;
        }

        var closeBrace = template.IndexOf('}', start + 1);
        if (closeBrace < 0 || template.IndexOfAny(['\r', '\n'], start, closeBrace - start) >= 0)
        {
            return false;
        }

        token = template[(start + 1)..closeBrace];
        next = closeBrace + 1;
        return true;
    }

    private static string StripLineBreaks(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}