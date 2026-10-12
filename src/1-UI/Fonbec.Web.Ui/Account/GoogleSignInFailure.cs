using Microsoft.AspNetCore.Authentication;

namespace Fonbec.Web.Ui.Account;

public static class GoogleSignInFailure
{
    public static Task Handle(RemoteFailureContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fonbec.Web.Ui.GoogleSignIn");
        logger.LogWarning(context.Failure, "Google sign-in was not completed.");

        var code = ToRemoteErrorCode(context.Request.Query["error"].ToString(), context.Failure?.Message);
        context.Response.Redirect(RedirectPath(context.Properties?.RedirectUri, code));
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

    public static string RedirectPath(string? authenticationRedirectUri, string remoteErrorCode)
    {
        var target = authenticationRedirectUri?.Contains("/Account/Manage/ExternalLogins", StringComparison.OrdinalIgnoreCase) == true
            ? "/Account/Manage/ExternalLogins"
            : "/Account/ExternalLogin";
        return $"{target}?RemoteError={Uri.EscapeDataString(remoteErrorCode)}";
    }

    private static bool IsCancellation(string? value) =>
        !string.IsNullOrEmpty(value)
        && (value.Contains("access_denied", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Access was denied", StringComparison.OrdinalIgnoreCase));
}