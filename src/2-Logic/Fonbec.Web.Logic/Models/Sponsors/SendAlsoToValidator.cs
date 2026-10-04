using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Models.Sponsors;

public readonly record struct SendAlsoToValidationItem(string? Name, string? Email);

public static class SendAlsoToValidator
{
    public const string RequiredMessage = "El nombre y el correo electrónico son obligatorios.";

    public const string InvalidEmailMessage = "Correo inválido.";

    public const string DuplicateMessage = "Hay correos electrónicos duplicados entre los destinatarios adicionales.";

    public const string MatchesPrimaryMessage = "El correo de un destinatario adicional no puede coincidir con el del padrino.";

    public const string NameTooLongMessage = $"El nombre no puede superar los 81 caracteres.";

    public const string EmailTooLongMessage = $"El correo electrónico no puede superar los 128 caracteres.";

    public const string NotAvailableMessage = "El padrino no está disponible.";

    public const string InactiveMessage = "El padrino está inactivo. No se pueden modificar los destinatarios.";

    public const string SaveFailedMessage = "No se pudieron guardar los destinatarios.";

    public static IReadOnlyList<string> Validate(
        IEnumerable<SendAlsoToValidationItem> recipients,
        string? primaryEmail)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalizedPrimary = NormalizeEmail(primaryEmail);

        foreach (var recipient in recipients)
        {
            var name = recipient.Name?.Trim() ?? string.Empty;
            var email = recipient.Email?.Trim() ?? string.Empty;

            if (name.Length == 0 || email.Length == 0)
            {
                errors.Add(RequiredMessage);
                continue;
            }

            if (name.Length > MaxLength.SendAlsoTo.Name)
            {
                errors.Add(NameTooLongMessage);
            }

            if (email.Length > MaxLength.FonbecWebUser.Email)
            {
                errors.Add(EmailTooLongMessage);
                continue;
            }

            if (!ContactFieldValidator.IsValidEmail(email))
            {
                errors.Add(InvalidEmailMessage);
                continue;
            }

            var normalized = email.ToLower();
            if (normalized.Length > 0 && normalized == normalizedPrimary)
            {
                errors.Add(MatchesPrimaryMessage);
            }

            if (!seen.Add(normalized))
            {
                errors.Add(DuplicateMessage);
            }
        }

        return errors.Distinct().ToList();
    }

    public static string? ValidateEmailField(
        string? email,
        string? primaryEmail,
        IEnumerable<string?> otherRecipientEmails)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > MaxLength.FonbecWebUser.Email)
        {
            return EmailTooLongMessage;
        }

        if (!ContactFieldValidator.IsValidEmail(trimmed))
        {
            return InvalidEmailMessage;
        }

        var normalized = trimmed.ToLower();
        if (normalized == NormalizeEmail(primaryEmail))
        {
            return MatchesPrimaryMessage;
        }

        if (otherRecipientEmails.Any(other => NormalizeEmail(other) == normalized))
        {
            return DuplicateMessage;
        }

        return null;
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLower();
}