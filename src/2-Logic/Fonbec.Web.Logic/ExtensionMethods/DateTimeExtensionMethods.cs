using System.Globalization;

namespace Fonbec.Web.Logic.ExtensionMethods;

public static class DateTimeExtensionMethods
{
    public static string ToLocalizedDateTime(this DateTime dateTime) =>
        dateTime.ToString(@"d/MM/yyyy \a \l\a\s HH:mm", new CultureInfo("es-AR"));
}