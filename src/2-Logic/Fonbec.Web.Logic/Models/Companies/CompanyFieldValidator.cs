using System.Text.RegularExpressions;

namespace Fonbec.Web.Logic.Models.Companies;

public static partial class CompanyFieldValidator
{
    private const int MinimumPhoneDigits = 7;
    private const int MaximumPhoneDigits = 15;

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Any(char.IsLetter);

    public static bool IsValidEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) || EmailRegex().IsMatch(email.Trim());

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true;
        }

        var trimmedPhone = phone.AsSpan().Trim();
        var digitCount = 0;

        foreach (var character in trimmedPhone)
        {
            if (char.IsDigit(character))
            {
                digitCount++;
                continue;
            }

            if (character is not ('+' or '-' or '(' or ')' or ' ' or '.'))
            {
                return false;
            }
        }

        return digitCount is >= MinimumPhoneDigits and <= MaximumPhoneDigits;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}