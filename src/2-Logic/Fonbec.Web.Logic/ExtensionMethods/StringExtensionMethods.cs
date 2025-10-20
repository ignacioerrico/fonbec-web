using System.Globalization;

namespace Fonbec.Web.Logic.ExtensionMethods;

public static class StringExtensionMethods
{
    public static bool ContainsIgnoringAccents(this string source, string subString)
    {
        const CompareOptions compareOptions = CompareOptions.IgnoreCase
                                              | CompareOptions.IgnoreSymbols
                                              | CompareOptions.IgnoreNonSpace;

        var index = CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, subString, compareOptions);

        return index != -1;
    }

    /// <summary>
    /// Normalizes the input text by trimming whitespace, removing extra spaces, 
    /// and converting each word to title case (first letter capitalized, rest lowercase).
    /// </summary>
    /// <param name="source">
    /// The input string to normalize. If the input is <c>null</c> or consists only of whitespace, 
    /// an empty string is returned.
    /// </param>
    /// <returns>
    /// A normalized string with trimmed whitespace, single spaces between words, 
    /// and each word in title case.
    /// </returns>
    public static string NormalizeText(this string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        // Normalize spacing and convert to title case
        var words = source
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLower()));

        return string.Join(' ', words);
    }
}