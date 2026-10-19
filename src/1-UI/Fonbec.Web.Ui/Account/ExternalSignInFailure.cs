using Microsoft.AspNetCore.Authentication;

namespace Fonbec.Web.Ui.Account;

public static class ExternalSignInFailure
{
    public static Func<RemoteFailureContext, Task> For(string providerName) =>
        context => HandleAsync(context, providerName);

    public static Task HandleAsync(RemoteFailureContext context, string providerName)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fonbec.Web.Ui.ExternalSignIn");
        logger.LogWarning(context.Failure, "{Provider} sign-in was not completed.", providerName);

        var code = ToRemoteErrorCode(context.Request.Query["error"].ToString(), context.Failure?.Message);
        context.Response.Redirect(RedirectPath(context.Properties?.RedirectUri, code, providerName));
        context.HandleResponse();
        return Task.CompletedTask;
    }

    public static string ToRemoteErrorCode(string? queryError, string? failureMessage)
    {
        if (IsCancellation(queryError) || IsCancellation(failureMessage))
        {
            return "access_denied";
        }

        return "error";
    }

    public static string RedirectPath(string? authenticationRedirectUri, string remoteErrorCode, string? providerName = null)
    {
        var target = authenticationRedirectUri?.Contains("/Account/Manage/ExternalLogins", StringComparison.OrdinalIgnoreCase) == true
            ? "/Account/Manage/ExternalLogins"
            : "/Account/ExternalLogin";
        var path = $"{target}?RemoteError={Uri.EscapeDataString(remoteErrorCode)}";
        if (!string.IsNullOrEmpty(providerName))
        {
            path += $"&Provider={Uri.EscapeDataString(providerName)}";
        }

        return path;
    }

    private static bool IsCancellation(string? value) =>
        !string.IsNullOrEmpty(value)
        && (value.Contains("access_denied", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Access was denied", StringComparison.OrdinalIgnoreCase));
}