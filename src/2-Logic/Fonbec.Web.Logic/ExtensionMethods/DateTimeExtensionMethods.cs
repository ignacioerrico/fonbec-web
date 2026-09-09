using System.Globalization;

namespace Fonbec.Web.Logic.ExtensionMethods;

public static class DateTimeExtensionMethods
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static string ToLocalizedDateTime(this DateTime dateTime) =>
        dateTime.ToString(@"d/MM/yyyy \a \l\a\s HH:mm", EsAr);

    public static string ToSpanishMonthYear(this DateTime dateTime) =>
        dateTime.ToString(@"MMMM \d\e yyyy", EsAr).CapitalizeFirstLetter();
}