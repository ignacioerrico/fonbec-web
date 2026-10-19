using System.Text.RegularExpressions;

namespace Fonbec.Web.Ui.Components.Errors;

public static partial class ExceptionLogSanitizer
{
    public static string StripQuery(string? uriOrPath)
    {
        if (string.IsNullOrEmpty(uriOrPath))
        {
            return string.Empty;
        }

        var end = uriOrPath.Length;
        var query = uriOrPath.IndexOf('?');
        if (query >= 0)
        {
            end = query;
        }

        var fragment = uriOrPath.IndexOf('#');
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }

        return uriOrPath[..end];
    }

    public static string RedactMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var redacted = EmailPattern().Replace(message, "[redacted]");
        redacted = BearerPattern().Replace(redacted, "Bearer [redacted]");
        return AssignmentPattern().Replace(redacted, "$1$2[redacted]");
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(
        @"\b(password|pwd|token|secret|apikey|api_key|connectionstring)\b(\s*[=:]\s*)(\S+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentPattern();
}