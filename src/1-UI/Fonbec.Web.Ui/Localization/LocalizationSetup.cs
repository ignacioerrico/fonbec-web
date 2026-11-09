using System.Globalization;
using Microsoft.AspNetCore.Localization;
using MudBlazor;

namespace Fonbec.Web.Ui.Localization;

public static class LocalizationSetup
{
    public static IServiceCollection AddFonbecLocalization(this IServiceCollection services)
    {
        services.AddLocalization(o => o.ResourcesPath = "Resources");
        // Stateless: translations read CurrentUICulture at lookup time, not registration time.
        services.AddSingleton<MudLocalizer, FonbecMudLocalizer>();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("es-AR", "es");
            options.SupportedCultures = [new("es-AR"), new("es"), new("en")];
            options.SupportedUICultures = [new("es"), new("en")];
            // Browser language must not override the Spanish production default.
            options.RequestCultureProviders = [new QueryStringRequestCultureProvider(), new CookieRequestCultureProvider()];
        });
        return services;
    }

    public static IApplicationBuilder UseFonbecLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();
        return app.Use(async (context, next) =>
        {
            if (CultureInfo.CurrentCulture.Name == "es")
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");

            // Carry explicit dev language choices across SSR posts, redirects and
            // the Interactive Server connection without adding a profile setting.
            var requested = context.Request.Query["culture"].ToString();
            if (requested is "es" or "es-AR" or "en")
            {
                context.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(
                        CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture)),
                    new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps, Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/" });
            }
            await next(context);
        });
    }
}
