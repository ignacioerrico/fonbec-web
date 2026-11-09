using System.Globalization;
using System.Resources;

namespace Fonbec.Web.Ui.Localization;

public static class ValidationResources
{
    private static readonly ResourceManager Resources = new(
        "Fonbec.Web.Ui.Resources.Localization.SharedResources", typeof(SharedResources).Assembly);

    public static string Required => Resources.GetString("ValidationRequired", CultureInfo.CurrentUICulture)!;
    public static string Email => Resources.GetString("ValidationEmail", CultureInfo.CurrentUICulture)!;
}
