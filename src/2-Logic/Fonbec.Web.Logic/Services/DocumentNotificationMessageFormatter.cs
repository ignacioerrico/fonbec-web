namespace Fonbec.Web.Logic.Services;

public static class DocumentNotificationMessageFormatter
{
    public static string FormatRejectionReason(string? description, string? notes)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.IsNullOrWhiteSpace(notes) ? "No se indicó un motivo." : notes.Trim();
        }

        return string.IsNullOrWhiteSpace(notes)
            ? description.Trim()
            : $"{description.Trim()}: {notes.Trim()}";
    }
}
