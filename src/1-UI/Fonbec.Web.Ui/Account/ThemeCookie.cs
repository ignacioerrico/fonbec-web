namespace Fonbec.Web.Ui.Account;

/// <summary>
/// Device theme cookie. Login pages read it without authentication, and logout leaves it in place.
/// Not HttpOnly: the signed-in toggle updates it from the browser so it can replace the value written at login.
/// </summary>
public static class ThemeCookie
{
    public const string Name = "Fonbec.Theme";
    public const string Light = "light";
    public const string Dark = "dark";

    public static bool IsDark(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && value == Dark;

    public static string Format(bool isDark) => isDark ? Dark : Light;

    public static void Set(HttpResponse response, bool isDark)
    {
        response.Cookies.Append(Name, Format(isDark), new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = response.HttpContext.Request.IsHttps,
            HttpOnly = false,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
        });
    }

    /// <summary>
    /// Keeps post-login and theme-toggle redirects on this site.
    /// </summary>
    public static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "~/";
        }

        var path = returnUrl.Trim();
        if (path.StartsWith("//", StringComparison.Ordinal)
            || path.Contains('\\')
            || IsOffSiteAbsolute(path))
        {
            return "~/";
        }

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return "~" + path;
    }

    private static bool IsOffSiteAbsolute(string path)
    {
        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // On Linux, Uri.TryCreate treats a rooted path such as "/Account/Manage" as a file URI.
        // That path is still local. A real file URL includes a scheme in the text.
        return uri.Scheme != Uri.UriSchemeFile
            || path.Contains("://", StringComparison.Ordinal)
            || path.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
    }
}