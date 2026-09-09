using System.Text.RegularExpressions;

namespace Fonbec.Web.Logic.Util;

public static partial class ContactFieldValidator
{
    private const int MinimumPhoneDigits = 7;
    private const int MaximumPhoneDigits = 15;

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

    public static string? ValidateEmail(string? email) =>
        IsValidEmail(email) ? null : "Correo inválido.";

    public static string? ValidatePhone(string? phone) =>
        IsValidPhone(phone) ? null : "Número de teléfono inválido.";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}